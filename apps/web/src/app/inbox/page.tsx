"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { api } from "@/lib/api";
import { Button, Card, Input, Shell } from "@/components/ui";
import type { IngestAccepted, PurchaseCandidate } from "@keepwise/shared";
import { useRouter } from "next/navigation";

const statusLabel: Record<number, string> = {
  0: "Processing",
  1: "Needs review",
  2: "Confirmed",
  3: "Ignored",
  4: "Failed",
  5: "Duplicate",
  6: "Needs OCR"
};

export default function InboxPage() {
  const router = useRouter();
  const [rows, setRows] = useState<PurchaseCandidate[]>([]);
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function load() {
    setRows(await api.candidates());
  }

  useEffect(() => {
    void load().catch((err: Error) => setError(err.message));
  }, []);

  async function onText(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const result = await api.ingestText(text);
      await afterIngest(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Import failed");
    } finally {
      setBusy(false);
    }
  }

  async function onFile(file: File) {
    setBusy(true);
    setError(null);
    try {
      const result = await api.ingestDocument(file);
      await afterIngest(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed");
    } finally {
      setBusy(false);
    }
  }

  async function afterIngest(result: IngestAccepted) {
    await load();
    if (result.candidateId) {
      router.push(`/inbox/${result.candidateId}`);
    }
  }

  return (
    <Shell>
      <h1 className="mb-4 text-2xl font-semibold">Purchase inbox</h1>
      <p className="mb-4 text-sm text-zinc-600">
        Paste an order SMS/email or upload a receipt PDF or photo. Keepwise extracts a candidate — you confirm before anything is saved as an asset.
      </p>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <form onSubmit={onText} className="space-y-3">
            <label className="text-sm">
              Shared order text
              <textarea
                className="mt-1 h-32 w-full rounded-md border border-zinc-300 p-2 text-sm"
                value={text}
                onChange={(e) => setText(e.target.value)}
                placeholder="Amazon.in order confirmation..."
              />
            </label>
            <Button type="submit" disabled={busy || !text.trim()}>
              Extract from text
            </Button>
          </form>
        </Card>
        <Card>
          <p className="mb-2 text-sm font-medium">Scan receipt or PDF</p>
          <Input
            type="file"
            accept="application/pdf,image/jpeg,image/png,image/webp"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) {
                void onFile(file);
              }
            }}
          />
        </Card>
      </div>
      {error ? <p className="mt-3 text-sm text-rose-700">{error}</p> : null}
      <div className="mt-6 space-y-3">
        {rows.length === 0 ? (
          <Card>
            <p className="text-sm text-zinc-500">No imported purchases yet.</p>
          </Card>
        ) : (
          rows.map((row) => (
            <Link key={row.id} href={`/inbox/${row.id}`}>
              <Card className="hover:border-[var(--brand)]">
                <div className="flex justify-between gap-3">
                  <div>
                    <p className="font-medium">{row.payload.productName ?? "Untitled purchase"}</p>
                    <p className="text-sm text-zinc-500">
                      {[row.payload.vendor, row.payload.purchaseDate, row.payload.amount != null ? `₹${row.payload.amount}` : null]
                        .filter(Boolean)
                        .join(" · ")}
                    </p>
                  </div>
                  <span className="text-xs">{statusLabel[row.status]}</span>
                </div>
              </Card>
            </Link>
          ))
        )}
      </div>
    </Shell>
  );
}
