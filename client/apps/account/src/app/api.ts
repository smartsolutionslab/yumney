// MFE facade. This is the only place the account MFE imports from
// @yumney/shared/api-client. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports from the shared lib out of account/**.
export {
  UserProfileApiService,
  ActivityApiService,
  type UserProfile,
  type UpdateProfileRequest,
  type UserActivityItem,
  type UserActivityPage,
  type ActivityTypeKey,
} from '@yumney/shared/api-client';
