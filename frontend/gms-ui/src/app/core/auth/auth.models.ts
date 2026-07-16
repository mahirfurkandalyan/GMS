/**
 * Frontend auth contract — mirrors the backend AuthController DTOs EXACTLY. Do not invent fields.
 * Backend: AuthUserDto (id, fullName, email, roles, permissions) and AuthResponse (accessToken,
 * refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt, user).
 */
export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
