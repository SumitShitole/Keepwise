"use client";

import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { api, setToken } from "@/lib/api";
import { Button, Input } from "@/components/ui";

export default function HomePage() {
  const router = useRouter();
  const [email, setEmail] = useState("sumit@keepwise.app");
  const [name, setName] = useState("Sumit");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const result = await api.devLogin(email, name);
      setToken(result.accessToken);
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not sign in.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-lg flex-col justify-center px-4">
      <p className="text-sm font-medium uppercase tracking-wide text-[var(--brand)]">Keepwise</p>
      <h1 className="mt-2 text-3xl font-semibold">Never miss a warranty or service date</h1>
      <p className="mt-3 text-zinc-600">
        Add the things you own. Keepwise calculates expiry dates and reminds you before they come due.
      </p>
      <form onSubmit={onSubmit} className="mt-8 space-y-4 rounded-xl border border-zinc-200 bg-white p-6 shadow-sm">
        <h2 className="text-lg font-medium">Continue with email</h2>
        <p className="text-sm text-zinc-600">
          Development login issues a passwordless session. Production uses Firebase email link and Google Sign-In.
        </p>
        <label className="block text-sm">
          Name
          <Input value={name} onChange={(e) => setName(e.target.value)} required className="mt-1" />
        </label>
        <label className="block text-sm">
          Email
          <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required className="mt-1" />
        </label>
        {error ? <p className="text-sm text-rose-700">{error}</p> : null}
        <Button type="submit" disabled={loading} className="w-full">
          {loading ? "Signing in…" : "Send me in"}
        </Button>
      </form>
    </div>
  );
}
