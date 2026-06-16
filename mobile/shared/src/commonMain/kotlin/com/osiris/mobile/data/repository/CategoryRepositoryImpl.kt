package com.osiris.mobile.data.repository

import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.dto.CategoryEditDto
import com.osiris.mobile.data.dto.CategoryListItemDto
import com.osiris.mobile.data.dto.CreateCategoryRequest
import com.osiris.mobile.data.dto.UpdateCategoryRequest
import com.osiris.mobile.data.network.osirisCatching
import com.osiris.mobile.data.remote.CategoryApi
import com.osiris.mobile.domain.model.Category
import com.osiris.mobile.domain.model.CategoryType
import com.osiris.mobile.domain.repository.CategoryRepository

class CategoryRepositoryImpl(private val api: CategoryApi) : CategoryRepository {

    override suspend fun list(): OsirisResult<List<Category>> = osirisCatching {
        api.list().map { it.toDomain() }
    }

    override suspend fun get(id: String): OsirisResult<Category> = osirisCatching {
        api.get(id).toDomain()
    }

    override suspend fun create(name: String, type: CategoryType, color: String?): OsirisResult<Unit> = osirisCatching {
        api.create(CreateCategoryRequest(name, type.apiValue, color))
        Unit
    }

    override suspend fun update(id: String, name: String, type: CategoryType, color: String?): OsirisResult<Unit> = osirisCatching {
        api.update(id, UpdateCategoryRequest(name, type.apiValue, color))
    }

    override suspend fun archive(id: String): OsirisResult<Unit> = osirisCatching {
        api.archive(id)
    }

    override suspend fun delete(id: String): OsirisResult<Unit> = osirisCatching {
        api.delete(id)
    }
}

private fun CategoryListItemDto.toDomain() =
    Category(id, name, CategoryType.fromApi(type), color, isActive)

private fun CategoryEditDto.toDomain() =
    Category(id, name, CategoryType.fromApi(type), color, isActive = true)
