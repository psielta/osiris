# Design — Agente por voz (Gemini Live API) no Osiris

> **Status:** proposta de design (sem código ainda). Escrito em 23/06/2026.
> **Escopo:** adicionar um **agente por voz** em tempo real (Web e mobile) **reaproveitando** toda a
> camada de tools/propostas do agente de texto já existente (`docs/ai-agent-blueprint.md`).
> **Princípio mantido:** o agente roda no backend; chave/SDK/prompt nunca vão ao cliente; tools executam
> server-side isoladas por tenant; escrita só por proposta confirmável.

Este documento define contratos, protocolo, segurança, custo, infraestrutura, testes e fases.

> **Implementação — Fase 1, passo A (fundação) — 23/06/2026.** Já no código (atrás da flag `AiAssistantVoice`,
> desligada; sem efeito no runtime atual): núcleo de execução de tool extraído para `IAiToolCallExecutor`
> (reusado pelo orquestrador de texto **e** pela voz — o `AiAgentOrchestrator` foi refatorado para usá-lo sem
> mudança de comportamento); `IAiLiveToolDispatcher` (batch para a sessão de voz); contratos provider-neutros
> `IAiLiveSessionClient`/`IAiLiveSession`/eventos (`AiLiveContracts.cs`); flag `Features:AiAssistantVoice`. 6
> testes novos; suíte 708 verde.
>
> **Passo B (servidor) — 23/06/2026.** No código (flag off): adapter `GeminiLiveSessionClient`/`GeminiLiveSession`
> (WebSocket `BidiGenerateContent`, chave server-side, sends serializados por lock, barge-in/goAway mapeados);
> **`GeminiLiveMessageParser`** puro (wire → eventos neutros) com **6 testes**; endpoint `GET /assistant/voice`
> no Web (`AiVoiceController`) com relay de dois pumps (cliente↔Gemini), gating de flag (**off → 404**, verificado)
> e auth (**não-autenticado → redirect**, verificado); `UseWebSockets`; `Gemini:LiveModel` + flag
> `Features:AiAssistantVoice`; `AiModelToolResult.Id` para correlacionar o `functionResponse`. **716 verde.**
> **Verificável agora:** parser + gating/auth do endpoint.
>
> **Passo C (áudio no navegador) — 23/06/2026 (v0.20.0).** No código (flag off): AudioWorklets
> `capture-worklet.js` (mic→PCM16 16 kHz, contexto criado a 16 kHz) e `playback-worklet.js` (fila PCM16 24 kHz
> com `flush` p/ barge-in); `voice-client.js` (`window.OsirisVoice.create`: abre o WS, streaming contínuo de
> áudio, toca a resposta, repassa eventos JSON); **botão de microfone** no `_AssistantWidget` (só quando a flag
> está on) com legenda ao vivo (transcrição) e barra de status; mapeamento de env `AI_ASSISTANT_VOICE` no
> compose. Build verde; o widget continua renderizando com a flag off (132 testes Web verdes).
> **Modelo live validado ao vivo (23/06/2026, v0.20.1).** Smoke test dirigindo `GeminiLiveSessionClient` contra
> o Gemini real provou que os ids `*-flash-live-preview` (incl. o default antigo) **não respondem**; o que
> funciona é **`gemini-2.5-flash-native-audio-preview-12-2025`** (retornou áudio + transcrição "tudo certo" +
> turnComplete). Default corrigido em `GeminiOptions`/appsettings; override por env `GEMINI_LIVE_MODEL` no
> compose. **Confirmado server→Gemini:** conexão, setup, function declarations aceitas, parser de áudio/transcrição.
> Falta só o teste de **áudio no navegador** (mic) pelo usuário. **Falta na trilha:** mobile (passo D), escrita
> por voz (Fase 2), persistência da conversa de voz e hardening (orçamento de áudio, resumption, métricas).

---

## 1. Objetivo e não-objetivos

### 1.1 Objetivo
- Conversa por **voz** (áudio↔áudio) com o assistente financeiro, em **Web e mobile**, mantendo também o
  chat de texto atual.
- **Reaproveitar** as 36 tools (16 leitura + 20 escrita) (`IAiTool`), o registry/policy, o protocolo de proposta e a auditoria.
- Preservar integralmente o modelo de segurança do blueprint (Seção 17).

### 1.2 Não-objetivos (desta entrega)
- Executar escrita financeira por comando de voz sem confirmação explícita.
- Vídeo (a Live API aceita imagens ≤1 fps, mas fica fora do MVP de voz).
- Ephemeral tokens / conexão cliente-direto ao Google (avaliado, **adiado** — ver Seção 3.1).
- Diarização/identificação de locutor, wake-word, telefonia.

---

## 2. Fatos da Live API que orientam o design

| Item | Valor |
|---|---|
| Transporte | WebSocket (WSS), sessão **stateful** |
| Áudio de entrada | PCM 16-bit, **16 kHz**, mono, little-endian |
| Áudio de saída | PCM 16-bit, **24 kHz**, mono, little-endian |
| Outras entradas | texto; imagens ≤1 fps (não usado no MVP) |
| Function calling | suportado na sessão; **2.5 Flash Live = async (`NON_BLOCKING`)**, **3.1 Flash Live = sequencial** |
| Modelos live | `gemini-2.5-flash-live-preview` (primário), `gemini-2.5-flash-native-audio-preview-12-2025` (áudio nativo), `gemini-3.1-flash-live-preview` |
| Contexto | ~32k tokens (live padrão), 128k (native audio) |
| Sessão | conexão ~10 min e sessão áudio ~15 min; **session resumption** estende (token de resumption ~2h); servidor emite `GoAway` antes de encerrar (confirmar números na doc de session management) |
| Conexão | **server-to-server (proxy)** ou **client-to-server** (com ephemeral tokens) |

**Modelo escolhido:** half-cascade com **async function calling** (não trava o áudio enquanto a tool roda),
essencial para um agente com muitas tools. **O id exato deve ser validado via Models API/AI Studio na
implementação** — `gemini-2.5-flash-live-preview` pode estar desatualizado; a página de modelos lista
`gemini-2.5-flash-native-audio-preview-12-2025` para o 2.5 Flash Live. Não hardcode o id (já é config em
`Gemini:LiveModel`). `3.1-flash-live` é descartado para escrita por ser sequencial.

> **Endpoint/auth por modo (confirmar na implementação):** server-to-server usa API key em
> `…/v1beta/…:BidiGenerateContent?key=…`; cliente-direto usa ephemeral token em
> `…/v1alpha/…:BidiGenerateContentConstrained?access_token=…`. Como o MVP é proxy, usamos o primeiro.

---

## 3. Decisões-chave de arquitetura

### 3.1 Proxy no servidor (escolhido) vs cliente-direto
A Live API permite o cliente conectar **direto** ao Google (menor latência, com ephemeral tokens). Porém,
no modo direto **o `functionCall` chega ao cliente** — e as tools do Osiris **precisam** rodar no servidor
(isolamento por tenant via `ICurrentUser`, MediatR, redaction, auditoria, escrita-por-proposta).

➡️ **Decisão: proxy servidor↔servidor.** O cliente faz WebSocket para o **backend do Osiris**; o backend
segura o WebSocket do Gemini (chave server-side) e relaya áudio. Os `functionCall` são executados pela
**mesma pilha de tools**. Isso preserva 100% do modelo de segurança e reaproveita o máximo.

> Ephemeral-token/cliente-direto fica como **otimização futura apenas para voz somente-leitura** (sem
> tools de escrita), onde o risco é menor. Não no MVP.

### 3.2 Áudio é o esforço real, não o "cérebro"
A pilha do agente já existe. O custo de implementação concentra-se em **captura/resample/playback de PCM**
nos clientes (Web `AudioWorklet`; Android `AudioRecord`/`AudioTrack`) e no **relay WebSocket**.

### 3.3 Voz não relaxa a confirmação de escrita
Toda ação de escrita continua virando `AiActionProposal` e exige confirmação **explícita** (toque no card).
Voz pode *criar* a proposta e *narrar* o impacto, mas **não executa** sem o confirm já existente.

---

## 4. Mapa de reaproveitamento

| Componente existente | Reuso no voz |
|---|---|
| `IAiTool` (36 tools (16 leitura + 20 escrita)) | `Name`/`Description`/`InputSchema` viram `tools.functionDeclarations` no `setup` |
| `IAiToolRegistry.GetAllowedTools` | mesma seleção por flag de writes |
| `IAiToolExecutionPolicy` | mesmo gating por risco |
| `AiAgentOrchestrator.ExecuteSingleAsync` (lógica) | extrair para um `AiLiveToolDispatcher` reutilizável (find→policy→execute→redact→record→propostas) |
| `IAiActionProposalFactory` + confirm/reject | propostas criadas na voz; confirmação pelos endpoints atuais |
| `AiPromptBuilder` | `systemInstruction` da sessão live (mesmo prompt versionado + nota de "modo voz") |
| `AiAgentContext` | montado do `ICurrentUser` no início da sessão (tenant/today/writesEnabled) |
| `AiDataRedactor`, `AiToolCall` (auditoria), orçamento de tokens | idênticos |
| `IAiModelClient` | **NÃO** serve (é request/response); ver Seção 5 |

Estimativa: ~90% do "cérebro" reusado; o novo é transporte + áudio.

---

## 5. Novos contratos (Application — provider-neutro)

```csharp
// Sessão live provider-neutra. O adapter Gemini fica em Infrastructure.
public interface IAiLiveSessionClient
{
    Task<IAiLiveSession> ConnectAsync(AiLiveSessionRequest request, CancellationToken ct);
}

public interface IAiLiveSession : IAsyncDisposable
{
    Task SendAudioAsync(ReadOnlyMemory<byte> pcm16, CancellationToken ct);   // 16kHz do cliente
    Task SendTextAsync(string text, CancellationToken ct);                   // entrada por texto opcional
    Task SendToolResultsAsync(IReadOnlyList<AiModelToolResult> results, CancellationToken ct);
    IAsyncEnumerable<AiLiveServerEvent> ReadEventsAsync(CancellationToken ct); // áudio out, toolCall, transcrições, goAway
}

public sealed record AiLiveSessionRequest(
    string SystemPrompt,
    IReadOnlyList<AiToolDefinition> Tools,
    AiLiveAudioConfig Audio,           // voz, modalidades de saída
    string CorrelationId);

public abstract record AiLiveServerEvent;
public sealed record AiLiveAudioChunk(ReadOnlyMemory<byte> Pcm24) : AiLiveServerEvent;
public sealed record AiLiveToolCall(IReadOnlyList<AiModelToolCall> Calls) : AiLiveServerEvent;
public sealed record AiLiveTranscript(string Text, bool IsUser, bool Final) : AiLiveServerEvent;
public sealed record AiLiveTurnComplete : AiLiveServerEvent;
public sealed record AiLiveGoAway(int MillisLeft) : AiLiveServerEvent;
```

O dispatcher de tools (reaproveita a lógica do orquestrador):

```csharp
public interface IAiLiveToolDispatcher
{
    Task<AiLiveToolBatchResult> DispatchAsync(
        AiAgentContext context, IReadOnlyList<AiModelToolCall> calls, CancellationToken ct);
    // retorna AiModelToolResult[] + sources + proposals (idêntico ao turno de texto)
}
```

`AiModelToolCall`/`AiModelToolResult`/`AiToolDefinition` já existem em `Common/AI` e são reusados.

---

## 6. Protocolo WebSocket cliente ↔ backend Osiris

Endpoint: `wss://osiris.mateussalgueiro.com.br/assistant/voice` (Web, cookie auth) e
`wss://osiris-api.mateussalgueiro.com.br/api/v1/ai/voice` (mobile, JWT). Mensagens **binárias** para áudio,
**texto JSON** para controle.

Cliente → servidor:
| Tipo | Conteúdo |
|---|---|
| binário | frame PCM16 16kHz (áudio do microfone) |
| `{"type":"start","conversationId":...}` | inicia/retoma conversa |
| `{"type":"text","content":...}` | entrada por texto (modo híbrido) |
| `{"type":"confirm","proposalId":...}` | confirma proposta (ou usa o endpoint REST atual) |
| `{"type":"stop"}` | encerra |

Servidor → cliente:
| Tipo | Conteúdo |
|---|---|
| binário | frame PCM16 24kHz (áudio do assistente) |
| `{"type":"transcript","role":...,"text":...,"final":...}` | legenda em tempo real |
| `{"type":"proposal","proposal":{...}}` | card de proposta para confirmar |
| `{"type":"sources","items":[...]}` | fontes citadas |
| `{"type":"status","value":"thinking|listening|goingaway"}` | estado |
| `{"type":"error","message":...}` | erro amigável |

O backend **nunca** repassa frames brutos do Gemini sem mediar function calls e propostas.

---

## 7. Integração com o Gemini Live (Infrastructure)

Adapter `GeminiLiveSessionClient` abre `wss://generativelanguage.googleapis.com/ws/...` com a chave em
header server-side. Fluxo:

1. **setup** (primeira mensagem): `model`, `generationConfig` (responseModalities=`["AUDIO"]` + transcrição),
   `systemInstruction` (do `AiPromptBuilder`), `tools.functionDeclarations` (do registry), config de voz.
2. **realtimeInput**: streaming de `audio` (PCM16 16kHz base64) conforme chega do cliente.
3. **toolCall** (do servidor Gemini): vira `AiLiveToolCall` → `IAiLiveToolDispatcher.DispatchAsync` →
   `toolResponse`/`functionResponses` de volta. `NON_BLOCKING` é **opt-in por function declaration**
   (`behavior: NON_BLOCKING`) e a resposta carrega `scheduling` (`INTERRUPT`/`WHEN_IDLE`/`SILENT`). **Tools de
   leitura** podem ser `NON_BLOCKING` (áudio segue enquanto roda); **tools de escrita** devem ser
   **blocking** ou `WHEN_IDLE` — senão o modelo pode narrar a proposta antes de ela existir.
4. **serverContent**: chunks de áudio 24kHz → `AiLiveAudioChunk`; transcrições; `turnComplete`.
5. **session resumption** + `GoAway`: reabrir sessão preservando contexto; sinalizar `goingaway` ao cliente.

> Os nomes exatos de campos do wire (`BidiGenerateContentSetup`, `realtimeInput`, `toolCall`,
> `toolResponse`, `sessionResumptionUpdate`, `goAway`) devem ser confirmados contra a doc vigente na
> implementação — a página de *session* estava indisponível na redação deste doc.

---

## 8. Escrita por proposta na voz (segurança da ação)

- Tools de risco `WriteProposal` continuam criando `AiActionProposal` (TTL, idempotência, state hash).
- Na voz: o assistente **narra** o impacto ("Vou criar uma conta a pagar de R$ 1.200; confirme na tela") e
  o cliente mostra o **card**; a execução só ocorre via `POST /actions/{id}/confirm` (toque).
- **Não** habilitar confirmação só por voz no MVP (risco de falso positivo em finanças). Reavaliar com
  dupla-confirmação falada auditável em fase futura.

---

## 9. Segurança e isolamento por tenant

- Auth do WebSocket = a mesma de hoje (cookie no Web, JWT na API); `AiAgentContext` vem do `ICurrentUser`.
- Chave Gemini só no servidor; cliente nunca recebe token do Google (modo proxy).
- Tenant nunca vem do modelo; ids retornados por voz são revalidados pelas tools.
- Redaction aplicada às transcrições/áudio-logs persistidos; **não** persistir áudio bruto por padrão.
- Prompt-injection por áudio: tratado como dado (mesma regra do texto); catálogo de tools fechado.

---

## 10. Configuração e flags

```jsonc
"Features": { "AiAssistantVoice": false },          // desligado por padrão
"Gemini": { "LiveModel": "gemini-2.5-flash-live-preview", "LiveVoice": "..." },
"AiAssistant": {
  "VoiceConnectMaxMinutes": 10,                       // limite de conexão (reabrir via resumption)
  "VoiceSessionMaxMinutes": 30,                       // teto lógico da sessão (várias conexões)
  "VoiceDailyAudioSecondsPerTenant": 1800,           // orçamento de áudio separado
  "VoiceMaxConcurrentSessionsPerUser": 1,            // uma sessão de voz por usuário
  "VoiceWritesEnabled": false                         // escrita por voz começa OFF
}
```

---

## 11. Áudio nos clientes

### 11.1 Web
- `getUserMedia` → `AudioContext` + **`AudioWorkletProcessor`**: captura, downsample para **16 kHz mono PCM16**,
  envia frames binários pelo WS. Playback do 24kHz via `AudioWorklet`/`AudioBufferSourceNode`.
- Componente Alpine/JS isolado; botão de microfone no `_AssistantWidget` (push-to-talk ou VAD simples).

### 11.2 Mobile (KMP)
- `expect/actual`: Android usa `AudioRecord` (16kHz) + `AudioTrack` (24kHz); WebSocket do **Ktor**.
- ViewModel de voz em `commonMain`; captura/playback em `androidMain`. iOS fica como milestone futuro.

---

## 12. Custo, limites e quotas
- Áudio consome muito mais tokens que texto → **orçamento de áudio separado por tenant/dia** e teto de
  duração de sessão. Métricas de segundos de áudio in/out por tenant.
- Sessão tem janela limitada; usar **session resumption** para continuidade e avisar `goingaway`.
- Contexto ~32k: manter histórico curto + resumo (igual ao texto).

---

## 13. Persistência e auditoria
- Reusar `AiConversation`/`AiMessage`/`AiToolCall`. Mensagens de voz gravam **transcrição** (não áudio),
  com `Model`, tokens (incl. áudio), `FinishReason`.
- `AiToolCall` registra cada execução durante a sessão (mesma redaction).
- Novo campo opcional: `Channel` (`text`|`voice`) em `AiMessage` para telemetria.

---

## 14. Infraestrutura
- **WebSocket pelo Caddy do TRX**: upgrade WS passa transparente; revisar timeouts de idle/read.
- `osiris-web` e `osiris-api` precisam habilitar WebSockets (ASP.NET `UseWebSockets`).
- Sem novo container; sem migração de schema obrigatória (exceto o campo `Channel`, opcional).
- Conexões de saída WSS para `generativelanguage.googleapis.com` a partir dos containers.

---

## 15. Estratégia de testes
- **Unit**: `AiLiveToolDispatcher` (reaproveita os fakes atuais — `MapSender`, `FakeAiActionProposalRepository`);
  parsing de eventos do adapter com fixtures de mensagens Live sanitizadas.
- **Integração**: handshake do WS autenticado (401 sem auth, flag-off → 404), criação de proposta na sessão,
  isolamento por tenant. Sem rede real (fake `IAiLiveSessionClient`).
- **Manual/ao vivo**: smoke test de áudio ponta a ponta contra o Gemini real (como já fazemos via harness),
  fora do CI.
- Reusar a `Osiris.Ai.EvaluationTests` para garantir que o catálogo de tools no `setup` bate com o de texto.

---

## 16. Plano de fases
1. **Fase 1 — Spike Web, somente-leitura**: `IAiLiveSessionClient` + `GeminiLiveSessionClient` + endpoint WS
   no `Osiris.Web` + áudio no navegador + `AiLiveToolDispatcher` com as **read tools**. Prova o pipeline.
2. **Fase 2 — Escrita por voz como card**: habilita `WriteProposal` na voz → propostas confirmáveis na tela.
3. **Fase 3 — Mobile**: KMP audio (`expect/actual`) + WS na API.
4. **Fase 4 — Hardening**: orçamento de áudio, session resumption, métricas/custo, (opcional) ephemeral-token
   para leitura, voz selecionável.

---

## 17. Decisões em aberto
1. Voz selecionável e idioma fixo pt-BR no `systemInstruction`?
2. Push-to-talk vs VAD (detecção de fala) no MVP — PTT é mais simples e barato.
3. `gemini-2.5-flash-live-preview` (half-cascade, async tools) vs `native-audio` (voz melhor, tools menos
   maduras) — começar pelo half-cascade.
4. Transcrição persistida sempre, ou só sob opt-in do usuário?
5. Web transport: WebSocket cru vs **SignalR** (reconexão/backpressure prontos) — provável SignalR no Web,
   WS cru/Ktor no mobile.

## 18. Riscos
| Risco | Mitigação |
|---|---|
| Latência/eco no áudio | PTT no MVP, `AudioWorklet` correto, jitter buffer pequeno |
| Custo de áudio explode | orçamento por tenant + teto de sessão + flag OFF por padrão |
| Modelo live em *preview* muda contrato | adapter isolado em Infrastructure; abstração provider-neutra |
| Escrita indevida por voz | proposta + confirmação por toque (sem confirm só-voz no MVP) |
| Sessão cai (GoAway) | session resumption + reconexão no cliente |
| WS atrás do Caddy com timeout | ajustar timeouts; heartbeats/ping |

---

## 19. Barge-in / interrupção (novo)
Voz precisa de política explícita de interrupção:
- O usuário falar durante a resposta = **barge-in**: parar o playback imediatamente, **descartar o áudio
  bufferizado** ainda não tocado e sinalizar `INTERRUPT` ao modelo.
- Tool calls **em voo** quando há barge-in: cancelar (cancellation token) read tools; **nunca** cancelar a
  persistência de uma proposta já criada (mas a narração pode ser interrompida).
- Definir VAD (server-side do Gemini) vs PTT no cliente — PTT no MVP simplifica barge-in.

## 20. Backpressure, capacidade e DoS (novo)
O proxy dobra conexões/tráfego; sem limites vira superfície de DoS:
- **Bounded channels** (System.Threading.Channels) em ambas as direções, com descarte/medição quando cheio.
- **Max bytes por frame** e **tamanho máximo de fila**; `WebSocketOptions` com limites de buffer.
- **Ping/pong** e timeout de idle; cancelamento ponta a ponta.
- **Rate limit** de sessões e de segundos de áudio por **usuário/tenant/IP** (além do orçamento diário).
- **Uma sessão de voz por usuário** (`VoiceMaxConcurrentSessionsPerUser=1`).
- Teste de **carga** antes de ligar em produção (Kestrel + Caddy timeouts).

## 21. Erro e reconexão (novo)
- `GoAway` → reabrir com **resumption handle** preservando contexto; avisar `goingaway` ao cliente.
- Queda do Gemini / `429` / `5xx` / áudio inválido → mensagem amigável + fallback para o chat de **texto**.
- Reconexão do cliente (web/mobile) com backoff; idempotência de `start`.

## 22. Hardening do WebSocket (detalhe — novo)
- Autenticação **no handshake/upgrade** (cookie no Web, JWT na API); rejeitar antes do upgrade.
- **Origin allowlist** + **nonce anti-CSRF** no Web (WS não tem o antiforgery padrão por header).
- **Confirmação de proposta de preferência pelo endpoint REST atual** (`POST /actions/{id}/confirm`), não por
  mensagem WS crua — reusa o antiforgery/idempotência já testados.
- `conversationId` sempre revalidado contra tenant/user; **resumption handle nunca cru ao cliente** (escopado
  por tenant/user/conversation e com expiração).
- Flag-off → handshake responde como recurso inexistente.

## 23. Privacidade da transcrição (novo)
A transcrição de voz é **dado financeiro sensível**:
- Não persistir áudio bruto por padrão; transcrição entra na retenção/exportação/exclusão já existentes.
- Avaliar criptografia em repouso / controle de acesso para transcrições; logs mínimos (sem conteúdo).

## 24. Observabilidade (novo — vira bloqueador em voz)
Métricas mínimas para operar voz: segundos de áudio in/out, profundidade de fila, close codes, latência p95
(captura→primeiro áudio), latência por tool, ciclo de vida de proposta e custo por sessão/tenant.

---

## 25. Changelog do design
- **23/06/2026 — revisão externa (Codex CLI):** corrigida a contagem de tools (**36** = 16 leitura + 20
  escrita, não 37); modelo live marcado para validação (não hardcodar id); limites de sessão separados
  (conexão ~10 min vs sessão ~15 min, resumption ~2h); `NON_BLOCKING` detalhado (opt-in + `scheduling`,
  blocking/`WHEN_IDLE` para escrita); endpoints por modo (`v1beta key` vs `v1alpha access_token`); e
  adicionadas as seções 19–24 (barge-in, backpressure/DoS, erro/reconexão, hardening do WS, privacidade da
  transcrição, observabilidade). Nota de implementação: `AiAgentOrchestrator.ExecuteSingleAsync` é privado —
  **extrair** para `AiLiveToolDispatcher` reutilizável.
