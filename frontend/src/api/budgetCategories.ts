import { api } from '@/api/client'
import type {
  BudgetCategory,
  CategoryEligibility,
  CreateBudgetCategoryRequest,
  UpdateBudgetCategoryRequest,
  UserLookup,
} from '@/types/api'

export const budgetCategoriesApi = {
  active: () =>
    api.get<BudgetCategory[]>('/budget-categories/active').then((r) => r.data),

  availableToMe: () =>
    api.get<BudgetCategory[]>('/budget-categories/available-to-me').then((r) => r.data),
}

export const adminBudgetCategoriesApi = {
  list: () =>
    api.get<BudgetCategory[]>('/admin/budget-categories').then((r) => r.data),

  create: (body: CreateBudgetCategoryRequest) =>
    api.post<BudgetCategory>('/admin/budget-categories', body).then((r) => r.data),

  update: (id: string, body: UpdateBudgetCategoryRequest) =>
    api.patch<BudgetCategory>(`/admin/budget-categories/${id}`, body).then((r) => r.data),
}

export const adminCategoryEligibilityApi = {
  list: (categoryId: string) =>
    api
      .get<CategoryEligibility[]>('/admin/category-eligibility', { params: { categoryId } })
      .then((r) => r.data),

  grant: (body: { userId: string; categoryId: string }) =>
    api.post<CategoryEligibility>('/admin/category-eligibility', body).then((r) => r.data),

  revoke: (id: string) => api.delete(`/admin/category-eligibility/${id}`),
}

export const adminUsersApi = {
  search: (q: string) =>
    api
      .get<UserLookup[]>('/admin/users/search', { params: { q } })
      .then((r) => r.data),
}
