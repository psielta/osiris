# Importar extrato em OFX

A importação por OFX evita digitar lançamento por lançamento. Você baixa o extrato da sua conta no banco e o Osiris transforma cada movimentação em um lançamento da conta.

> OFX é um formato de extrato que quase todos os bancos brasileiros oferecem para baixar.

## O que é o arquivo OFX

OFX é um arquivo (final `.ofx`) que o seu banco gera com as movimentações de um período: data, valor e descrição de cada entrada e saída.

Quase todo internet banking ou aplicativo do banco tem uma opção de **exportar/baixar extrato** em OFX. Procure por algo como "Exportar extrato", "Baixar OFX" ou "OFX (Money/Quicken)".

## Como importar

A importação acontece em duas etapas, para você revisar antes de salvar:

1. Na conta, escolha **Importar OFX** e selecione o arquivo `.ofx` baixado do banco.
2. O Osiris lê o arquivo e mostra a lista de lançamentos encontrados.
3. Revise a lista, escolha as categorias e confirme.

No computador, abra a conta e clique em **Importar OFX**. No celular, abra o extrato da conta e toque em **Importar**.

## Revisar antes de confirmar

Na tela de revisão você decide o que entra:

- **Marcar e desmarcar:** cada linha tem uma caixa de seleção. Desmarque o que não quiser importar.
- **Categoria:** escolha a categoria de cada lançamento, ou use "Categoria para todos" para aplicar a mesma a todos de uma vez. A categoria é opcional — você pode categorizar depois no extrato.
- **Tipo:** entradas viram receita e saídas viram despesa, conforme o sinal do valor no extrato.

Nada é salvo até você confirmar.

## Lançamentos repetidos não entram duas vezes

O Osiris identifica cada movimentação pelo código único que o banco coloca no arquivo. Se você importar o mesmo extrato de novo, ou um período que se sobrepõe a outro já importado, os lançamentos repetidos aparecem como **"Já importado"** e vêm desmarcados.

Assim você pode reimportar sem medo de duplicar o saldo.

## Depois de importar

Os lançamentos importados entram no extrato da conta como qualquer outro lançamento, e o saldo da conta é atualizado na hora.

Como todo lançamento, eles **não podem ser editados nem excluídos** depois. Se algo veio errado do banco, registre um lançamento de correção no sentido contrário, como faria com um lançamento manual.

## O que ainda não é importado

- **Extrato de cartão de crédito.** A importação é para o extrato de uma conta (banco, poupança, dinheiro). Compras do cartão continuam sendo registradas na área de cartões.
- **Categorias automáticas.** O Osiris não adivinha a categoria pela descrição; você escolhe na hora de importar ou depois.

## Resumo

1. Baixe o extrato em OFX no seu banco.
2. Abra a conta e escolha Importar OFX.
3. Revise, escolha categorias e confirme.
4. Reimporte quando quiser: os repetidos não entram de novo.
