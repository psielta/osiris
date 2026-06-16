package com.osiris.mobile.data.session

class FakeTokenStore(initial: TokenBundle? = null) : TokenStore {
    var bundle: TokenBundle? = initial
        private set

    override suspend fun read(): TokenBundle? = bundle

    override suspend fun save(bundle: TokenBundle) {
        this.bundle = bundle
    }

    override suspend fun clear() {
        bundle = null
    }
}
