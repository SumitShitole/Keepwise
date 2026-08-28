"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Button, Card, Input, Shell } from "@/components/ui";
import type { Category } from "@keepwise/shared";

export default function NewItemPage() {
  const router = useRouter();
  const [categories, setCategories] = useState<Category[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    api.categories().then(setCategories).catch((err: Error) => setError(err.message));
  }, []);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setLoading(true);
    setError(null);
    try {
      const durationYears = Number(form.get("warrantyYears") || 0);
      const explicitExpiry = String(form.get("explicitExpiry") || "");
      const purchaseDate = String(form.get("purchaseDate") || "");
      if (explicitExpiry && !purchaseDate) {
        setError("Add a purchase date on or before the warranty expiry.");
        setLoading(false);
        return;
      }
      if (explicitExpiry && purchaseDate && explicitExpiry < purchaseDate) {
        setError("Warranty expiry cannot be earlier than the start date.");
        setLoading(false);
        return;
      }
      const created = await api.createItem({
        name: form.get("name"),
        categoryId: form.get("categoryId") || null,
        brand: form.get("brand") || null,
        modelNumber: form.get("modelNumber") || null,
        purchaseDate: form.get("purchaseDate") || null,
        purchasePrice: form.get("purchasePrice") ? Number(form.get("purchasePrice")) : null,
        currency: "INR",
        vendorName: form.get("vendorName") || null,
        notes: form.get("notes") || null,
        warranty:
          durationYears || explicitExpiry
            ? {
                kind: 0,
                startDate: form.get("purchaseDate") || null,
                durationValue: explicitExpiry ? null : durationYears || 1,
                durationUnit: explicitExpiry ? null : 3,
                explicitEndDate: explicitExpiry || null
              }
            : null
      });
      router.push(`/items/${created.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save item.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <Shell>
      <h1 className="mb-4 text-2xl font-semibold">Add item</h1>
      <Card>
        <form onSubmit={onSubmit} className="grid gap-4 sm:grid-cols-2">
          <label className="text-sm sm:col-span-2">
            Name
            <Input name="name" required placeholder="Samsung washing machine" className="mt-1" />
          </label>
          <label className="text-sm">
            Category
            <select name="categoryId" className="mt-1 w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm">
              <option value="">Select…</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </label>
          <label className="text-sm">
            Brand
            <Input name="brand" className="mt-1" />
          </label>
          <label className="text-sm">
            Model
            <Input name="modelNumber" className="mt-1" />
          </label>
          <label className="text-sm">
            Purchase date
            <Input name="purchaseDate" type="date" className="mt-1" />
          </label>
          <label className="text-sm">
            Price (INR)
            <Input name="purchasePrice" type="number" min="0" step="1" className="mt-1" />
          </label>
          <label className="text-sm">
            Vendor
            <Input name="vendorName" className="mt-1" />
          </label>
          <label className="text-sm">
            Warranty years
            <Input name="warrantyYears" type="number" min="0" defaultValue="2" className="mt-1" />
          </label>
          <label className="text-sm">
            Or explicit expiry
            <Input name="explicitExpiry" type="date" className="mt-1" />
          </label>
          <label className="text-sm sm:col-span-2">
            Notes
            <Input name="notes" className="mt-1" />
          </label>
          {error ? <p className="text-sm text-rose-700 sm:col-span-2">{error}</p> : null}
          <div className="sm:col-span-2">
            <Button type="submit" disabled={loading}>
              {loading ? "Saving…" : "Save item"}
            </Button>
          </div>
        </form>
      </Card>
    </Shell>
  );
}
