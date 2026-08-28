import Link from "next/link";

export function Button({
  children,
  className = "",
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      className={`inline-flex items-center justify-center rounded-md bg-[var(--brand)] px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50 ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}

export function Input({ className = "", ...props }: React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={`w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm outline-none ring-[var(--brand)] focus:ring-2 ${className}`}
      {...props}
    />
  );
}

export function Card({ children, className = "" }: { children: React.ReactNode; className?: string }) {
  return <div className={`rounded-xl border border-zinc-200 bg-white p-4 shadow-sm ${className}`}>{children}</div>;
}

export function StatusChip({ status }: { status: number | null }) {
  const map: Record<number, { label: string; className: string }> = {
    0: { label: "Active", className: "bg-emerald-100 text-emerald-800" },
    1: { label: "Expiring soon", className: "bg-amber-100 text-amber-900" },
    2: { label: "Expired", className: "bg-rose-100 text-rose-800" },
    3: { label: "Extended", className: "bg-sky-100 text-sky-800" },
    4: { label: "Cancelled", className: "bg-zinc-200 text-zinc-700" }
  };
  if (status === null || status === undefined) {
    return <span className="text-xs text-zinc-500">No warranty</span>;
  }
  const chip = map[status] ?? map[0];
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${chip.className}`}>{chip.label}</span>;
}

export function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="mx-auto flex min-h-screen max-w-6xl flex-col px-4 py-6">
      <header className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <Link href="/dashboard" className="text-xl font-semibold tracking-tight text-[var(--brand)]">
          Keepwise
        </Link>
        <nav className="flex gap-4 text-sm">
          <Link href="/dashboard">Dashboard</Link>
          <Link href="/inbox">Inbox</Link>
          <Link href="/items">Items</Link>
          <Link href="/items/new">Add item</Link>
          <Link href="/settings">Settings</Link>
        </nav>
      </header>
      <main className="flex-1">{children}</main>
    </div>
  );
}
