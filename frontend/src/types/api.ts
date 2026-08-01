export type UserRole = 'User' | 'Admin'
export type RequestStatus = 'Pending' | 'Approved' | 'Rejected'

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
  userId: string
  email: string
  fullName: string
  role: UserRole | string
}

export interface MeResponse {
  userId: string
  email: string
  fullName: string
  role: UserRole | string
  hasTransactionPin: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  fullName: string
  email: string
  password: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface SetTransactionPinRequest {
  pin: string
  oldPin?: string
}

export interface BudgetCategory {
  id: string
  name: string
  defaultAllocatedAmount: number
  isTransferable: boolean
  isSelfRequestable: boolean
  isActive: boolean
  isSystemDefault: boolean
  createdAt: string
}

export interface CreateBudgetCategoryRequest {
  name: string
  defaultAllocatedAmount: number
  isTransferable: boolean
  isSelfRequestable: boolean
}

export interface UpdateBudgetCategoryRequest {
  defaultAllocatedAmount?: number
  isTransferable?: boolean
  isSelfRequestable?: boolean
  isActive?: boolean
}

export interface Account {
  id: string
  userId: string
  accountNumber: string
  balance: number
  currency: string
  accountType: string
  status: string
  createdAt: string
  categoryId?: string | null
  categoryName?: string | null
  isTransferable?: boolean
  ownerFullName?: string | null
  ownerEmail?: string | null
}

export interface AccountLookup {
  id: string
  accountNumber: string
  status: string
  ownerDisplayName: string
}

export interface Balance {
  accountId: string
  balance: number
  currency: string
}

export interface CreateAccountRequest {
  userId: string
  currency?: string
}

export interface AccountRequest {
  id: string
  userId: string
  userFullName?: string
  userEmail?: string
  categoryId: string
  categoryName?: string
  status: RequestStatus | string
  requestedAt: string
  reviewedAt?: string | null
  reviewedByUserId?: string | null
  rejectionReason?: string | null
  resultingAccountId?: string | null
}

export interface CardRequest {
  id: string
  userId: string
  userFullName?: string
  userEmail?: string
  accountId: string
  label?: string | null
  status: RequestStatus | string
  requestedAt: string
  reviewedAt?: string | null
  reviewedByUserId?: string | null
  rejectionReason?: string | null
  resultingCardId?: string | null
}

export interface TopUpRequest {
  id: string
  userId: string
  userFullName?: string
  userEmail?: string
  accountId: string
  amount: number
  note?: string | null
  status: RequestStatus | string
  requestedAt: string
  reviewedAt?: string | null
  reviewedByUserId?: string | null
  rejectionReason?: string | null
  resultingTransactionRecordId?: string | null
}

export interface SubmitCardRequest {
  accountId: string
  label?: string
}

export interface ApproveCardRequestResult {
  cardId: string
  lastFourDigits: string
  maskedNumber: string
  rawCardNumber: string
  label?: string | null
}

export interface Card {
  id: string
  accountId: string
  label?: string | null
  maskedNumber: string
  status: string
  issuedAt: string
  expiresAt: string
}

export interface IssueCardRequest {
  accountId: string
  cardNumber: string
  expiresAt: string
  label?: string
}

export interface MoneyOperationRequest {
  accountId: string
  amount: number
  description?: string
}

export interface AdjustmentRequest {
  accountId: string
  amount: number
  direction: 'Increase' | 'Decrease' | 0 | 1
  reason: string
}

export interface SpendRequest {
  accountId: string
  cardId: string
  amount: number
  description?: string
  pin: string
}

export interface TransferRequest {
  sourceAccountId: string
  destinationAccountId: string
  amount: number
  description?: string
  pin: string
}

export interface TransactionRecord {
  id: string
  type: string
  sourceAccountId: string
  destinationAccountId?: string | null
  cardId?: string | null
  performedByUserId?: string | null
  amount: number
  status: string
  transactionGroupId: string
  idempotencyKey: string
  description?: string | null
  createdAt: string
}

export interface AdminAccountListItem {
  id: string
  userId: string
  accountNumber: string
  ownerFullName: string
  ownerEmail: string
  categoryId?: string | null
  categoryName?: string | null
  balance: number
  currency: string
  status: string
  createdAt: string
}

export interface CategoryEligibility {
  id: string
  userId: string
  userFullName: string
  userEmail: string
  categoryId: string
  categoryName: string
  grantedByAdminUserId: string
  grantedAt: string
}

export interface UserLookup {
  id: string
  fullName: string
  email: string
  role: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
}
