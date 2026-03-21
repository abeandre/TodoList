export const Routes = {
  Home: 'home',
  Login: 'login',
  Register: 'register',
} as const;

export type RouteName = typeof Routes[keyof typeof Routes];
