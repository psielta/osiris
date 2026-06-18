# Faturas de cartão

Faturas reúnem as compras de um cartão em um ciclo de cobrança. Elas mostram quanto foi comprado, quanto já foi pago e quanto ainda está em aberto.

## Como as faturas aparecem

Você não precisa criar faturas manualmente. Elas são criadas quando compras são registradas no cartão.

O Osiris usa:

- data da compra;
- dia de fechamento do cartão;
- dia de vencimento do cartão;
- número de parcelas.

Com isso, cada compra ou parcela entra na fatura correta.

## Referência, fechamento e vencimento

Cada fatura tem três datas importantes:

| Campo | O que significa |
| --- | --- |
| Referência | Mês e ano da fatura |
| Fechamento | Data em que o ciclo fecha |
| Vencimento | Data limite para pagar |

Compras depois do fechamento entram na próxima fatura.

## Status da fatura

Os status ajudam a entender a situação:

| Status | Significado |
| --- | --- |
| Aberta | Ainda está no ciclo atual ou possui saldo em aberto |
| Fechada | O ciclo fechou e ainda existe valor a pagar |
| Parcialmente paga | Já houve pagamento, mas falta saldo |
| Paga | O valor total foi quitado |
| Vencida | Passou do vencimento e ainda existe saldo em aberto |

## Total, pago e em aberto

Na fatura, confira:

- **Total:** soma das compras e parcelas daquela fatura.
- **Pago:** soma dos pagamentos registrados.
- **Em aberto:** valor que ainda falta pagar.

Se o valor em aberto for zero, a fatura está quitada.

## Pagar fatura

Para pagar, abra a fatura e use a ação de pagamento. Você pode pagar o total ou registrar pagamentos parciais.

Campos principais:

| Campo | Como usar |
| --- | --- |
| Valor | Valor pago naquele momento |
| Data | Dia em que o pagamento aconteceu |
| Conta | Conta financeira de onde o dinheiro saiu |
| Observações | Informação opcional |

Quando você escolhe uma conta, o pagamento reduz o saldo dela.

## Pagamento não duplica gasto

O gasto por categoria já foi contado quando a compra foi registrada. O pagamento da fatura apenas quita a dívida e movimenta o caixa.

Por isso:

- a compra aparece nos gastos por categoria;
- o pagamento aparece na visão de caixa;
- o mesmo gasto não é contado duas vezes.

## Pagamentos parciais

Se você pagar apenas parte da fatura, registre o valor pago. A fatura fica parcialmente paga e mantém o saldo restante em aberto.

Quando pagar o restante, registre um novo pagamento. O histórico de pagamentos fica preservado.

## PDF da fatura

A tela da fatura permite exportar PDF. Use o PDF quando precisar guardar, enviar ou conferir uma fatura fora do sistema.

O PDF reflete os dados registrados no Osiris no momento da exportação.

## Quando revisar uma fatura

Revise a fatura quando:

- o total não bate com o banco;
- uma compra caiu no mês errado;
- uma parcela não apareceu;
- o pagamento foi feito, mas a fatura continua em aberto;
- o painel mostra fatura vencida.

Na maioria dos casos, a correção está na data da compra, no fechamento do cartão ou no pagamento que ainda não foi registrado.
