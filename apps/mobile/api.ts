import { createApiClient } from "@keepwise/shared";

const API = process.env.EXPO_PUBLIC_API_URL ?? "http://127.0.0.1:43124";

let token: string | null = null;

export function getToken(): string | null {
  return token;
}

export function setToken(value: string | null) {
  token = value;
}

export const api = createApiClient(API, getToken);
