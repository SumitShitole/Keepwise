"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, setToken } from "@/lib/api";
import { Button, Card, Input, Shell } from "@/components/ui";
import type { UserProfile } from "@keepwise/shared";
import { useRouter } from "next/navigation";

export default function SettingsPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api.me().then(setProfile).catch(() => router.replace("/"));
  }, [router]);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profile) {
      return;
    }
    const form = new FormData(event.currentTarget);
    const updated = await api.updateMe({
      displayName: String(form.get("displayName")),
      mobileNumber: String(form.get("mobileNumber") || "") || null,
      countryCode: String(form.get("countryCode") || "IN"),
      timeZoneId: String(form.get("timeZoneId") || "Asia/Kolkata"),
      language: "en",
      pushEnabled: form.get("pushEnabled") === "on",
      emailEnabled: form.get("emailEnabled") === "on",
      smsEnabled: false,
      whatsAppEnabled: false
    });
    setProfile(updated);
    setMessage("Saved.");
  }

  if (!profile) {
    return (
      <Shell>
        <p>Loading profile…</p>
      </Shell>
    );
  }

  return (
    <Shell>
      <h1 className="mb-4 text-2xl font-semibold">Settings</h1>
      <Card>
        <form onSubmit={onSubmit} className="grid max-w-xl gap-3">
          <label className="text-sm">
            Name
            <Input name="displayName" defaultValue={profile.displayName} className="mt-1" />
          </label>
          <label className="text-sm">
            Email
            <Input defaultValue={profile.email} disabled className="mt-1" />
          </label>
          <label className="text-sm">
            Mobile
            <Input name="mobileNumber" defaultValue={profile.mobileNumber ?? ""} className="mt-1" />
          </label>
          <label className="text-sm">
            Country
            <Input name="countryCode" defaultValue={profile.countryCode} className="mt-1" />
          </label>
          <label className="text-sm">
            Timezone
            <Input name="timeZoneId" defaultValue={profile.timeZoneId} className="mt-1" />
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" name="pushEnabled" defaultChecked={profile.pushEnabled} /> Push notifications
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" name="emailEnabled" defaultChecked={profile.emailEnabled} /> Email notifications
          </label>
          {message ? <p className="text-sm text-emerald-700">{message}</p> : null}
          <div className="flex gap-3">
            <Button type="submit">Save</Button>
            <Button
              type="button"
              className="bg-zinc-700"
              onClick={() => {
                setToken(null);
                router.push("/");
              }}
            >
              Sign out
            </Button>
          </div>
        </form>
      </Card>
    </Shell>
  );
}
