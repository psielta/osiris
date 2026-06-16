package com.osiris.mobile.domain.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CategoryType

interface CategoryRepository {
    suspend fun list(): OsirisResult<List<Category>>
    suspend fun get(id: String): OsirisResult<Category>
    suspend fun create(name: String, type: CategoryType, color: String?): OsirisResult<Unit>
    suspend fun update(id: String, name: String, type: CategoryType, color: String?): OsirisResult<Unit>
    suspend fun archive(id: String): OsirisResult<Unit>
    suspend fun delete(id: String): OsirisResult<Unit>
}
