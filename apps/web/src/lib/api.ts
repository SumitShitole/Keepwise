"use client";

import { createApiClient } from "@keepwise/shared";

const TOKEN_KEY = "keepwise.token";

export function getToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null) {
  if (token) {
    window.localStorage.setItem(TOKEN_KEY, token);
  } else {
    window.localStorage.removeItem(TOKEN_KEY);
  }
}

export const api = createApiClient(
  process.env.NEXT_PUBLIC_API_URL ?? "http://127.0.0.1:43124",
  getToken
);
