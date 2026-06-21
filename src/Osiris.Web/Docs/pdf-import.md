# Importar extrato em PDF (com IA)

Alguns bancos só entregam o extrato em **PDF**. A importação por PDF usa **inteligência artificial (IA)** para ler o arquivo e extrair os lançamentos automaticamente — você **não precisa mapear colunas** como no CSV. Depois é só revisar e confirmar.

> Diferente do OFX e do CSV, aqui a IA interpreta o PDF do jeito que o banco montou. Por isso vale conferir os valores extraídos antes de confirmar.

## Como funciona

Você envia o PDF do extrato e a IA identifica cada lançamento: data, descrição e valor (entrada ou saída). O Osiris monta a lista para você revisar — nada é salvo até você confirmar.

A leitura acontece em um serviço de IA na nuvem, então é preciso **conexão com a internet** e o processamento **pode levar alguns segundos**.

## Como importar

1. Na conta, escolha **Importar PDF (IA)** e selecione o arquivo `.pdf` do extrato.
2. Aguarde a IA ler o arquivo (alguns segundos).
3. Revise a lista de lançamentos, escolha as categorias e confirme.

No computador, abra a conta e clique em **Importar PDF (IA)**. No celular, abra o extrato da conta, toque em **Importar** e escolha **Importar PDF (IA)**.

## Revisar antes de confirmar

Como a IA pode errar em algum lançamento, **revise com atenção** antes de confirmar:

- **Confira valores e datas:** veja se os valores e o sentido (entrada ou saída) batem com o extrato.
- **Marcar e desmarcar:** desmarque o que não quiser importar.
- **Categoria:** escolha a categoria de cada lançamento, ou use "Categoria para todos". A categoria é opcional.

Nada é salvo até você confirmar.

## Lançamentos repetidos não entram duas vezes

O Osiris gera uma chave estável a partir de data, valor e descrição. Se você importar o mesmo período de novo (por OFX, CSV ou PDF), os repetidos aparecem como **"Já importado"** e vêm desmarcados. Assim dá para reimportar sem duplicar o saldo.

## Pontos de atenção

- **A IA pode errar.** Sempre confira os lançamentos extraídos antes de confirmar.
- **Precisa de internet** e pode levar alguns segundos.
- **PDFs protegidos por senha** não funcionam — remova a senha e tente de novo.
- O arquivo deve ter no máximo **15 MB**.

## Depois de importar

Os lançamentos entram no extrato da conta como qualquer outro, e o saldo é atualizado na hora. Como todo lançamento, eles **não podem ser editados nem excluídos** depois — se algo veio errado, registre um lançamento de correção no sentido contrário.

## Resumo

1. Baixe o extrato em PDF no seu banco.
2. Abra a conta e escolha Importar PDF (IA).
3. Aguarde a IA ler o arquivo.
4. Revise com atenção, escolha categorias e confirme.
5. Reimporte quando quiser: os repetidos não entram de novo.
