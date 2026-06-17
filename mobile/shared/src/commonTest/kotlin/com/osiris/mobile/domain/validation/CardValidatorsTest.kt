package com.osiris.mobile.domain.validation

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class CardValidatorsTest {

    @Test
    fun card_fields_validate_required_and_ranges() {
        assertEquals("Informe o nome do cartao.", CardValidators.name(""))
        assertEquals("Informe o limite.", CardValidators.money("", "limite"))
        assertEquals("O dia de fechamento deve estar entre 1 e 31.", CardValidators.day("40", "dia de fechamento"))

        assertNull(CardValidators.name("Nubank"))
        assertNull(CardValidators.money("1500,50", "limite"))
        assertNull(CardValidators.day("10", "dia de vencimento"))
    }

    @Test
    fun purchase_fields_validate_category_amount_and_installments() {
        assertEquals("Selecione a categoria da compra.", CardValidators.category(null))
        assertEquals("O valor total deve ser maior que zero.", CardValidators.positiveMoney("0", "valor total"))
        assertEquals("O numero de parcelas deve estar entre 1 e 120.", CardValidators.installments("121"))

        assertNull(CardValidators.category("category-id"))
        assertNull(CardValidators.positiveMoney("10", "valor total"))
        assertNull(CardValidators.installments("12"))
    }
}
