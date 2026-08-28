"use client";

import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { api } from "@/lib/api";
import { Button, Card, Input, Shell, StatusChip } from "@/components/ui";
import { coverageKindLabel, type ItemDetail } from "@keepwise/shared";

export default function ItemDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [item, setItem] = useState<ItemDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setItem(await api.item(params.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Not found");
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.id]);

  async function onDelete() {
    await api.deleteItem(params.id);
    router.push("/items");
  }

  async function addMaintenance(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await api.addCoverage(params.id, {
      kind: 1,
      startDate: form.get("startDate"),
      recurrenceValue: Number(form.get("months") || 6),
      recurrenceUnit: 2
    });
    await load();
  }

  if (error) {
    return (
      <Shell>
        <p className="text-rose-700">{error}</p>
      </Shell>
    );
  }

  if (!item) {
    return (
      <Shell>
        <p>Loading item…</p>
      </Shell>
    );
  }

  return (
    <Shell>
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{item.name}</h1>
          <p className="text-sm text-zinc-500">
            {[item.brand, item.modelNumber, item.categoryName].filter(Boolean).join(" · ")}
          </p>
        </div>
        <Button onClick={onDelete} className="bg-rose-700">
          Delete
        </Button>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <h2 className="mb-2 font-medium">Purchase</h2>
          <p className="text-sm">Date: {item.purchaseDate ?? "—"}</p>
          <p className="text-sm">
            Price: {item.purchasePrice != null ? `${item.currency} ${item.purchasePrice}` : "—"}
          </p>
          <p className="text-sm">Vendor: {item.vendorName ?? "—"}</p>
          <p className="mt-2 text-sm text-zinc-600">{item.notes ?? "No notes"}</p>
        </Card>
        <Card>
          <h2 className="mb-2 font-medium">Coverages</h2>
          {item.coverages.length === 0 ? (
            <p className="text-sm text-zinc-500">No warranty or maintenance yet.</p>
          ) : (
            <ul className="space-y-3">
              {item.coverages.map((coverage) => (
                <li key={coverage.id} className="rounded-md border border-zinc-100 p-3">
                  <div className="flex items-center justify-between">
                    <span className="font-medium">{coverageKindLabel[coverage.kind]}</span>
                    <StatusChip status={coverage.status} />
                  </div>
                  <p className="text-sm text-zinc-600">
                    {coverage.startDate} → {coverage.kind === 1 ? coverage.nextDueDate : coverage.endDate}
                  </p>
                  {coverage.kind === 1 && coverage.id ? (
                    <button
                      className="mt-2 text-sm text-[var(--brand)] underline"
                      onClick={async () => {
                        await api.completeMaintenance(coverage.id, new Date().toISOString().slice(0, 10));
                        await load();
                      }}
                    >
                      Mark maintenance complete
                    </button>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
          <form onSubmit={addMaintenance} className="mt-4 space-y-2 border-t pt-3">
            <p className="text-sm font-medium">Add maintenance</p>
            <Input name="startDate" type="date" required />
            <Input name="months" type="number" defaultValue="6" min="1" />
            <Button type="submit">Add recurring maintenance</Button>
          </form>
        </Card>
      </div>
    </Shell>
  );
}
