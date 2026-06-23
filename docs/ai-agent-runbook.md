# Runbook — Assistente de IA (Osiris)

> Operação do assistente de IA. Complementa `docs/ai-agent-blueprint.md` (arquitetura) — aqui ficam os procedimentos do dia a dia. As feature flags vêm desligadas por padrão.

## 1. Feature flags

Seção `Features` do appsettings (Web e Api). Tudo `false` por padrão.

| Flag | Efeito quando `true` |
| --- | --- |
| `Features:AiAssistant` | Liga os endpoints `/api/v1/ai/*` e a página Web `/assistant`. Com `false`, tudo responde 404, como se não existisse. |
| `Features:AiAssistantWrites` | Oferece as tools de proposta (`propose_*`). Com `false`, o assistente é somente leitura. |
| `Features:AiAssistantMobile` | Reservada para o app mobile (o app também tem a constante client-side `AssistantFeature.Enabled`). |

**Desligar em incidente:** defina `Features:AiAssistant=false` e recicle o app. O restante do Osiris (CRUDs, autenticação, relatórios, importação de PDF) não é afetado — a falha do Gemini nunca derruba o sistema.

## 2. Segredos e configuração

- **`Gemini__ApiKey`**: somente server-side, via variável de ambiente/secret store. Nunca em appsettings versionado, logs, browser ou APK.
- Modelos e limites em `Gemini` (AgentModel/FastModel/Temperature/MaxOutputTokens/timeouts) e `AiAssistant` (PromptVersion, limites de loop, TTL de proposta, retenção, orçamento diário de tokens).
- Produção deve usar tier pago e authorization key/restrições do Google (dados financeiros reais).

### Rotação da chave

1. Gere uma nova chave no projeto Google correspondente ao ambiente.
2. Atualize `Gemini__ApiKey` no secret store do ambiente.
3. Recicle o app (a chave é lida na inicialização do HttpClient).
4. Revogue a chave antiga após confirmar que o turno de teste responde.
5. Use projetos/chaves separados por ambiente.

## 3. Quotas e custo

- **Orçamento diário por tenant:** `AiAssistant:DailyTokenLimitPerTenant` (padrão 200.000). Ao exceder, o turno retorna `429`. O cálculo usa o usage persistido em `AiMessage` (input+output do dia).
- Mensagem máxima: `AiAssistant:MaxMessageCharacters` (4000).
- Limites por minuto/IP e turnos simultâneos ainda não estão implementados (ver Seção 0.3 do blueprint).

## 4. Retenção, exportação e exclusão

- Retenção alvo: `AiAssistant:ConversationRetentionDays` (90 dias). O purge automático ainda não foi implementado; até lá, excluir é manual/sob demanda.
- **Exportar:** `GET /api/v1/ai/conversations/{id}` retorna a conversa com mensagens (JSON).
- **Excluir (definitivo):** `DELETE /api/v1/ai/conversations/{id}` remove a conversa e tudo que ela referencia (mensagens, tool calls, propostas). Na Web, o botão 🗑️ na lista de conversas.
- **Arquivar (ocultar):** `POST /api/v1/ai/conversations/{id}/archive` — some da lista, mas continua visível e bloqueia novas mensagens.
- Não persistimos chain-of-thought; tool calls são auditados já redigidos.

## 5. Propostas de escrita

- Tools disponíveis (com `AiAssistantWrites` ligada): `propose_manual_movement`, `propose_bill_creation`, `propose_card_purchase`, `propose_bill_payment`, `propose_statement_payment`. (`propose_category_change` ainda não existe.)
- O modelo nunca executa um command financeiro no turno: cria uma `AiActionProposal` (Pending, TTL `AiAssistant:ProposalTtlMinutes`).
- Confirmação (`POST /api/v1/ai/actions/{id}/confirm`) revalida TTL + hash do estado-base e executa o command existente **uma vez** (idempotente). `stale`/`expired`/já resolvida → `409`.
- Rejeição: `POST /api/v1/ai/actions/{id}/reject`.

## 6. Sintomas e respostas

| Sintoma | Provável causa | Ação |
| --- | --- | --- |
| Endpoints `/ai/*` em 404 | `Features:AiAssistant=false` | Esperado se desligado; ligue a flag se deveria estar on. |
| `503` no turno | Gemini indisponível / chave inválida | Verifique `Gemini__ApiKey` e o status do provedor; o resto do Osiris segue funcionando. |
| `429` no turno | Orçamento diário do tenant estourado | Esperado; ajuste `DailyTokenLimitPerTenant` se necessário. |
| `409` ao confirmar | Proposta expirada/stale/já resolvida | Peça ao usuário para gerar nova proposta. |
| Resposta com número estranho | — | Números vêm só de tool results; cheque a fonte citada. Abra issue se persistir. |

## 7. Observabilidade

- Logs estruturados (Serilog) com correlation id, modelo, prompt version, tool name/risco, duração, tokens, status. **Nunca** logam chave/JWT, prompt completo, resposta financeira completa ou PDF.
- Redação aplicada a args/resultados de tool antes de persistir (`AiDataRedactor`).
- Spans OpenTelemetry (`ai.*`) e métricas/painel de custo ainda não implementados (follow-up).

## 8. Checklist de release

1. Tier pago + authorization key configurados no ambiente.
2. `Gemini__ApiKey` no secret store; rotação testada.
3. Flags definidas conscientemente (`AiAssistant` on; `AiAssistantWrites` apenas quando a confirmação estiver validada).
4. `dotnet test Osiris.sln` verde; testes mobile relevantes verdes.
5. Bump de versão do Web quando a UI for ligada para usuários (hoje está atrás de flag desligada).
