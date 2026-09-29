import { useMemo } from 'react';
import { groupPermissions } from '@/lib/permissions';
import type { EntityPermission } from '@/lib/permissions';
import type { Permission } from '@/lib/types/permission';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface Props {
  permissions: Permission[];
  selected: Set<string>;
  onChange: (next: Set<string>) => void;
  disabled?: boolean;
  idPrefix: string;
}

export function PermissionPicker({
  permissions,
  selected,
  onChange,
  disabled,
  idPrefix,
}: Props) {
  const grouped = useMemo(() => groupPermissions(permissions), [permissions]);

  const toggleOne = (permissionId: string) => {
    const next = new Set(selected);
    if (next.has(permissionId)) next.delete(permissionId);
    else next.add(permissionId);
    onChange(next);
  };

  const toggleGroup = (entries: EntityPermission[], selectAll: boolean) => {
    const next = new Set(selected);
    for (const entry of entries) {
      if (selectAll) next.add(entry.permissionId);
      else next.delete(entry.permissionId);
    }
    onChange(next);
  };

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {Object.entries(grouped).map(([entity, entries]) => {
        const selectedCount = entries.filter((entry) =>
          selected.has(entry.permissionId),
        ).length;
        const allSelected = selectedCount === entries.length;
        const groupState = allSelected
          ? true
          : selectedCount > 0
            ? 'indeterminate'
            : false;

        return (
          <Card key={entity} size="sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-3">
                <Checkbox
                  id={`${idPrefix}-group-${entity}`}
                  checked={groupState}
                  onCheckedChange={() => toggleGroup(entries, !allSelected)}
                  disabled={disabled}
                />
                <Label
                  htmlFor={`${idPrefix}-group-${entity}`}
                  className="text-base font-semibold capitalize"
                >
                  {entity}
                </Label>
                <span className="ml-auto text-xs font-normal text-muted-foreground">
                  {selectedCount}/{entries.length}
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="ml-2 space-y-3 border-l pl-5">
                {entries.map((entry) => (
                  <div
                    key={entry.permissionId}
                    className="flex items-start gap-3"
                  >
                    <Checkbox
                      id={`${idPrefix}-${entry.permissionId}`}
                      checked={selected.has(entry.permissionId)}
                      onCheckedChange={() => toggleOne(entry.permissionId)}
                      disabled={disabled}
                    />
                    <div className="flex-1">
                      <Label
                        htmlFor={`${idPrefix}-${entry.permissionId}`}
                        className="font-normal capitalize"
                      >
                        {entry.name}
                      </Label>
                      {entry.description && (
                        <p className="text-sm text-muted-foreground">
                          {entry.description}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
