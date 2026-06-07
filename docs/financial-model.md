# Financial Domain Model

Osiris separates actual spending from cash movement.

## Credit Cards

A credit card purchase is the categorized expense. If a purchase is split into
installments, each installment belongs to the original purchase and is grouped
into the relevant credit card statement.

A credit card statement is grouped debt. It collects card purchases and
installments for a billing cycle, but it is not itself a spending category.

A statement payment is debt settlement and account cash outflow. Paying a
statement must not create a second categorized expense, because the expense was
already recorded when the card purchase happened.

## Bills

A bill is an off-card obligation, such as rent, internet, school, gym, a bank
slip, or an off-card subscription. Bills are separate from credit card
statements.

## Reporting

The dashboard must keep two views distinct:

- Expense view: actual spending by category, based on purchases and off-card
  bills.
- Cash-flow view: money entering and leaving financial accounts, including
  statement payments.

Phase 0 documents this model and prepares shared infrastructure. Later phases
will implement the calculations and screens. Do not bake mutable financial
state into the authentication cookie; keep the cookie limited to stable identity
context such as the tenant id.
