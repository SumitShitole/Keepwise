"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, getToken } from "@/lib/api";
import { Card, Shell, StatusChip } from "@/components/ui";
import type { Dashboard } from "@keepwise/shared";
import { coverageKindLabel } from "@keepwise/shared";

export default function DashboardPage() {
  const router = useRouter();
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!getToken()) {
      router.replace("/");
      return;
    }
    api
      .dashboard()
      .then(setData)
      .catch((err: Error) => setError(err.message));
  }, [router]);

  if (error) {
    return (
      <Shell>
        <p className="text-rose-700">{error}</p>
      </Shell>
    );
  }

  if (!data) {
    return (
      <Shell>
        <p>Loading your reminders…</p>
      </Shell>
    );
  }

  const stats = [
    ["Items", data.totalItems],
    ["Active warranties", data.activeWarranties],
    ["Expiring soon", data.warrantiesExpiringSoon],
    ["Maintenance due", data.upcomingMaintenance],
    ["Renewals due", data.upcomingRenewals],
    ["Expired", data.expiredItems]
  ] as const;

  return (
    <Shell>
      <h1 className="mb-4 text-2xl font-semibold">Dashboard</h1>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {stats.map(([label, value]) => (
          <Card key={label}>
            <p className="text-sm text-zinc-500">{label}</p>
            <p className="mt-1 text-3xl font-semibold">{value}</p>
          </Card>
        ))}
      </div>
      <div className="mt-8 grid gap-6 lg:grid-cols-2">
        <Card>
          <h2 className="mb-3 font-medium">Upcoming</h2>
          {data.upcomingEvents.length === 0 ? (
            <p className="text-sm text-zinc-500">Nothing due in the next 30 days.</p>
          ) : (
            <ul className="space-y-2 text-sm">
              {data.upcomingEvents.map((event) => (
                <li key={event.coverageId} className="flex items-center justify-between gap-2">
                  <Link href={`/items/${event.itemId}`} className="font-medium hover:underline">
                    {event.itemName}
                  </Link>
                  <span>
                    {coverageKindLabel[event.kind]} · {event.date} <StatusChip status={event.status} />
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Card>
        <Card>
          <h2 className="mb-3 font-medium">Recently added</h2>
          {data.recentlyAdded.length === 0 ? (
            <p className="text-sm text-zinc-500">
              No items yet. <Link href="/items/new" className="text-[var(--brand)] underline">Add your first item</Link>
            </p>
          ) : (
            <ul className="space-y-2 text-sm">
              {data.recentlyAdded.map((item) => (
                <li key={item.id}>
                  <Link href={`/items/${item.id}`} className="hover:underline">
                    {item.name}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </Shell>
  );
}
