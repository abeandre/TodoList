export interface UserResponse {
  id: string;
  name: string;
  email: string;
  token?: string; // JWT token returned upon registration or login
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
