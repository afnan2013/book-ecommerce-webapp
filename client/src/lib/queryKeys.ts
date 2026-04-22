export const queryKeys = {
  auth: {
    me: ['auth', 'me'] as const,
  },
  users: {
    all: ['users'] as const,
    list: () => ['users', 'list'] as const,
    byId: (id: number) => ['users', id] as const,
  },
  roles: {
    all: ['roles'] as const,
    list: () => ['roles', 'list'] as const,
    byId: (id: number) => ['roles', id] as const,
  },
  permissions: {
    all: ['permissions'] as const,
    list: () => ['permissions', 'list'] as const,
  },
} as const;
