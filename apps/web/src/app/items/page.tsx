"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { Card, Input, Shell, StatusChip } from "@/components/ui";
import type { ItemSummary } from "@keepwise/shared";

export default function ItemsPage() {
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<ItemSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handle = setTimeout(() => {
      const query = search ? `?search=${encodeURIComponent(search)}` : "";
      api
        .items(query)
        .then((result) => {
          setItems(result.items);
          setTotal(result.total);
        })
        .catch((err: Error) => setError(err.message));
    }, 200);
    return () => clearTimeout(handle);
  }, [search]);

  return (
    <Shell>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">Items</h1>
        <Link href="/items/new" className="rounded-md bg-[var(--brand)] px-4 py-2 text-sm text-white">
          Add item
        </Link>
      </div>
      <Input placeholder="Search name, brand, model, vendor…" value={search} onChange={(e) => setSearch(e.target.value)} />
      {error ? <p className="mt-3 text-sm text-rose-700">{error}</p> : null}
      <p className="mt-3 text-sm text-zinc-500">{total} items</p>
      <div className="mt-4 space-y-3">
        {items.length === 0 ? (
          <Card>
            <p className="text-sm text-zinc-600">No items match this search. Add a purchase to start tracking dates.</p>
          </Card>
        ) : (
          items.map((item) => (
            <Link key={item.id} href={`/items/${item.id}`}>
              <Card className="hover:border-[var(--brand)]">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-medium">{item.name}</p>
                    <p className="text-sm text-zinc-500">
                      {[item.brand, item.categoryName].filter(Boolean).join(" · ") || "Uncategorized"}
                    </p>
                  </div>
                  <StatusChip status={item.warrantyStatus} />
                </div>
                {item.warrantyEndDate ? (
                  <p className="mt-2 text-sm text-zinc-600">Warranty until {item.warrantyEndDate}</p>
                ) : null}
              </Card>
            </Link>
          ))
        )}
      </div>
    </Shell>
  );
}
