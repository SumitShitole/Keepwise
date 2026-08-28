"use client";

import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { api } from "@/lib/api";
import { Button, Card, Input, Shell } from "@/components/ui";
import type { PurchaseCandidate } from "@keepwise/shared";

export default function CandidateReviewPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [row, setRow] = useState<PurchaseCandidate | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.candidate(params.id).then(setRow).catch((err: Error) => setError(err.message));
  }, [params.id]);

  async function onSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!row) {
      return;
    }
    const form = new FormData(event.currentTarget);
    const payload = {
      ...row.payload,
      productName: String(form.get("productName") || ""),
      vendor: String(form.get("vendor") || "") || null,
      brand: String(form.get("brand") || "") || null,
      purchaseDate: String(form.get("purchaseDate") || "") || null,
      amount: form.get("amount") ? Number(form.get("amount")) : null,
      orderNumber: String(form.get("orderNumber") || "") || null,
      warrantyDurationMonths: form.get("warrantyDurationMonths")
        ? Number(form.get("warrantyDurationMonths"))
        : null,
      returnWindowDays: form.get("returnWindowDays") ? Number(form.get("returnWindowDays")) : null
    };
    setRow(await api.editCandidate(row.id, payload));
  }

  async function confirm() {
    if (!row) {
      return;
    }
    const result = await api.confirmCandidate(row.id);
    router.push(`/items/${result.itemId}`);
  }

  async function ignore() {
    if (!row) {
      return;
    }
    await api.ignoreCandidate(row.id);
    router.push("/inbox");
  }

  if (error) {
    return (
      <Shell>
        <p className="text-rose-700">{error}</p>
      </Shell>
    );
  }

  if (!row) {
    return (
      <Shell>
        <p>Loading candidate…</p>
      </Shell>
    );
  }

  const p = row.payload;
  const editable = row.status === 1 || row.status === 5 || row.status === 6 || row.status === 4;

  return (
    <Shell>
      <h1 className="mb-2 text-2xl font-semibold">Review purchase</h1>
      <p className="mb-4 text-sm text-zinc-600">
        Confidence {Math.round(row.overallConfidence * 100)}%.
        {p.warrantyProvenance === 4 ? " Warranty is not confirmed from the document." : ""}
        {row.status === 5 ? " This looks like a duplicate of an existing purchase." : ""}
        {row.status === 6 ? " Image needs OCR (not configured in this environment)." : ""}
      </p>
      <Card>
        <form onSubmit={onSave} className="grid gap-3 sm:grid-cols-2">
          <label className="text-sm sm:col-span-2">
            Product
            <Input name="productName" defaultValue={p.productName ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Vendor
            <Input name="vendor" defaultValue={p.vendor ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Brand
            <Input name="brand" defaultValue={p.brand ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Purchase date
            <Input name="purchaseDate" type="date" defaultValue={p.purchaseDate ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Amount
            <Input name="amount" type="number" defaultValue={p.amount ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Order number
            <Input name="orderNumber" defaultValue={p.orderNumber ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Warranty months
            <Input name="warrantyDurationMonths" type="number" defaultValue={p.warrantyDurationMonths ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Return window days
            <Input name="returnWindowDays" type="number" defaultValue={p.returnWindowDays ?? ""} className="mt-1" />
          </label>
          {error ? <p className="text-sm text-rose-700 sm:col-span-2">{error}</p> : null}
          <div className="flex flex-wrap gap-2 sm:col-span-2">
            <Button type="submit" disabled={!editable} className="bg-zinc-700">
              Save edits
            </Button>
            <Button type="button" onClick={() => void confirm()} disabled={!editable}>
              Confirm
            </Button>
            <Button type="button" onClick={() => void ignore()} disabled={!editable} className="bg-rose-700">
              Ignore
            </Button>
          </div>
        </form>
      </Card>
    </Shell>
  );
}
