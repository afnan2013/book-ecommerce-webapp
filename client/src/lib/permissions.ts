import type { Permission } from './types/permission';

export interface EntityPermission {
  permissionId: string;
  name: string;
  description: string;
}

export type GroupedPermissions = Record<string, EntityPermission[]>;

function splitName(name: string): { entity: string; action: string } {
  const separator = name.indexOf('.');
  if (separator === -1) return { entity: 'other', action: name };
  return {
    entity: name.slice(0, separator),
    action: name.slice(separator + 1),
  };
}

export function groupPermissions(
  permissions: Permission[],
): GroupedPermissions {
  const grouped: GroupedPermissions = {};

  for (const permission of permissions) {
    const { entity, action } = splitName(permission.name);
    const entry: EntityPermission = {
      permissionId: permission.id,
      name: action,
      description: permission.description,
    };
    if (grouped[entity]) grouped[entity].push(entry);
    else grouped[entity] = [entry];
  }

  const sorted: GroupedPermissions = {};
  for (const entity of Object.keys(grouped).sort()) {
    sorted[entity] = grouped[entity].sort((a, b) =>
      a.name.localeCompare(b.name),
    );
  }
  return sorted;
}
