// MFE facade. This is the only place the account MFE imports from the
// per-backend api libs. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports out of account/**.
export { UserProfileApiService, type UserProfile, type UpdateProfileRequest } from '@yumney/shared/api-user-profile';
export { ActivityApiService, type UserActivityItem, type UserActivityPage, type ActivityTypeKey } from '@yumney/shared/api-dashboard';
