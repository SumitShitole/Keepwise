export type CoverageKind = 0 | 1 | 2 | 3;
export type CoverageStatus = 0 | 1 | 2 | 3 | 4;
export type DurationUnit = 0 | 1 | 2 | 3;

export const coverageKindLabel: Record<number, string> = {
  0: "Warranty",
  1: "Maintenance",
  2: "Renewal",
  3: "Return window"
};

export const coverageStatusLabel: Record<number, string> = {
  0: "Active",
  1: "Expiring soon",
  2: "Expired",
  3: "Extended",
  4: "Cancelled"
};

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  mobileNumber: string | null;
  countryCode: string;
  timeZoneId: string;
  language: string;
  pushEnabled: boolean;
  emailEnabled: boolean;
  smsEnabled: boolean;
  whatsAppEnabled: boolean;
}

export interface AuthResponse {
  accessToken: string;
  user: UserProfile;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  itemTypes: { id: string; name: string; slug: string }[];
}

export interface ReminderRule {
  id: string;
  offsetValue: number;
  offsetUnit: DurationUnit;
  isEnabled: boolean;
}

export interface Coverage {
  id: string;
  kind: CoverageKind;
  title: string | null;
  provider: string | null;
  referenceNumber: string | null;
  startDate: string;
  endDate: string;
  explicitEndDate: string | null;
  durationValue: number | null;
  durationUnit: DurationUnit | null;
  status: CoverageStatus;
  isCancelled: boolean;
  isExtended: boolean;
  recurrenceValue: number | null;
  recurrenceUnit: DurationUnit | null;
  nextDueDate: string | null;
  premium: number | null;
  notes: string | null;
  reminderRules: ReminderRule[];
}

export interface ItemSummary {
  id: string;
  name: string;
  brand: string | null;
  categoryName: string | null;
  purchaseDate: string | null;
  warrantyStatus: CoverageStatus | null;
  warrantyEndDate: string | null;
  nextMaintenanceDate: string | null;
  isArchived: boolean;
}

export interface ItemDetail {
  id: string;
  name: string;
  categoryId: string | null;
  categoryName: string | null;
  itemTypeId: string | null;
  itemTypeName: string | null;
  brand: string | null;
  modelNumber: string | null;
  serialNumber: string | null;
  purchaseDate: string | null;
  purchasePrice: number | null;
  currency: string;
  vendorName: string | null;
  vendorContact: string | null;
  notes: string | null;
  isArchived: boolean;
  coverages: Coverage[];
  attachments: { id: string; fileName: string; contentType: string; sizeBytes: number; createdAtUtc: string }[];
}

export interface PagedItems {
  items: ItemSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export interface Dashboard {
  totalItems: number;
  activeWarranties: number;
  warrantiesExpiringSoon: number;
  upcomingMaintenance: number;
  upcomingRenewals: number;
  expiredItems: number;
  upcomingEvents: {
    itemId: string;
    coverageId: string;
    itemName: string;
    kind: CoverageKind;
    date: string;
    status: CoverageStatus;
  }[];
  recentlyAdded: { id: string; name: string; createdAtUtc: string }[];
  attention: {
    kind: string;
    title: string;
    detail: string;
    href: string | null;
    urgency: number;
  }[];
  pendingCandidates: number;
}

export interface CandidatePayload {
  isPurchase: boolean;
  vendor: string | null;
  productName: string | null;
  brand: string | null;
  model: string | null;
  purchaseDate: string | null;
  amount: number | null;
  currency: string;
  orderNumber: string | null;
  invoiceNumber: string | null;
  warrantyDurationMonths: number | null;
  warrantyEndDate: string | null;
  serialNumber: string | null;
  gstin: string | null;
  upiReference: string | null;
  returnWindowDays: number | null;
  warrantyProvenance: number;
  overallConfidence: number;
  fieldConfidence: Record<string, number>;
}

export interface PurchaseCandidate {
  id: string;
  status: number;
  sourceType: number;
  overallConfidence: number;
  duplicateOfId: string | null;
  confirmedItemId: string | null;
  payload: CandidatePayload;
  createdAtUtc: string;
}

export interface IngestAccepted {
  jobId: string;
  candidateId: string | null;
  status: number;
}

export interface IngestionSettings {
  receiptScanningEnabled: boolean;
  sharedTextEnabled: boolean;
  emailScanningEnabled: boolean;
  smsImportEnabled: boolean;
  whatsAppImportEnabled: boolean;
  aiProcessingEnabled: boolean;
}

export function createApiClient(baseUrl: string, getToken: () => string | null) {
  async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const token = getToken();
    const headers = new Headers(init.headers);
    if (!headers.has("Content-Type") && init.body) {
      headers.set("Content-Type", "application/json");
    }
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }
    const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
    if (response.status === 204) {
      return undefined as T;
    }
    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || `Request failed (${response.status})`);
    }
    if (response.headers.get("content-type")?.includes("application/json")) {
      return (await response.json()) as T;
    }
    return undefined as T;
  }

  return {
    devLogin: (email: string, displayName?: string) =>
      request<AuthResponse>("/v1/auth/dev-login", {
        method: "POST",
        body: JSON.stringify({ email, displayName })
      }),
    me: () => request<UserProfile>("/v1/users/me"),
    updateMe: (body: Partial<UserProfile> & { displayName: string; countryCode: string; timeZoneId: string; language: string; pushEnabled: boolean; emailEnabled: boolean; smsEnabled: boolean; whatsAppEnabled: boolean }) =>
      request<UserProfile>("/v1/users/me", { method: "PUT", body: JSON.stringify(body) }),
    categories: () => request<Category[]>("/v1/catalog/categories"),
    dashboard: () => request<Dashboard>("/v1/dashboard"),
    items: (query = "") => request<PagedItems>(`/v1/items${query}`),
    item: (id: string) => request<ItemDetail>(`/v1/items/${id}`),
    createItem: (body: unknown) =>
      request<ItemDetail>("/v1/items", { method: "POST", body: JSON.stringify(body) }),
    deleteItem: (id: string) => request<void>(`/v1/items/${id}`, { method: "DELETE" }),
    addCoverage: (itemId: string, body: unknown) =>
      request<Coverage>(`/v1/items/${itemId}/coverages`, { method: "POST", body: JSON.stringify(body) }),
    completeMaintenance: (coverageId: string, eventDate: string) =>
      request<void>(`/v1/coverages/${coverageId}/complete`, {
        method: "POST",
        body: JSON.stringify({ eventDate })
      }),
    ingestText: (text: string, sourceType = 2) =>
      request<IngestAccepted>("/v1/ingestion/text", {
        method: "POST",
        body: JSON.stringify({ text, sourceType })
      }),
    ingestDocument: async (file: File) => {
      const token = getToken();
      const form = new FormData();
      form.append("file", file);
      const headers = new Headers();
      if (token) {
        headers.set("Authorization", `Bearer ${token}`);
      }
      const response = await fetch(`${baseUrl}/v1/ingestion/documents`, { method: "POST", body: form, headers });
      if (!response.ok) {
        throw new Error(await response.text());
      }
      return (await response.json()) as IngestAccepted;
    },
    candidates: (status?: number) =>
      request<PurchaseCandidate[]>(`/v1/purchase-candidates${status === undefined ? "" : `?status=${status}`}`),
    candidate: (id: string) => request<PurchaseCandidate>(`/v1/purchase-candidates/${id}`),
    editCandidate: (id: string, payload: CandidatePayload) =>
      request<PurchaseCandidate>(`/v1/purchase-candidates/${id}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      }),
    confirmCandidate: (id: string) =>
      request<{ itemId: string }>(`/v1/purchase-candidates/${id}/confirm`, { method: "POST" }),
    ignoreCandidate: (id: string) =>
      request<void>(`/v1/purchase-candidates/${id}/ignore`, { method: "POST" }),
    ingestionSettings: () => request<IngestionSettings>("/v1/users/me/ingestion-settings"),
    updateIngestionSettings: (body: IngestionSettings) =>
      request<IngestionSettings>("/v1/users/me/ingestion-settings", {
        method: "PUT",
        body: JSON.stringify(body)
      }),
    privacy: () => request<{ ingestion: IngestionSettings; pendingCandidates: number; importedDocuments: number; aiProcessingEnabled: boolean }>("/v1/privacy")
  };
}

export function daysUntil(date: string, today = new Date()): number {
  const target = new Date(`${date}T00:00:00Z`);
  const start = Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate());
  return Math.round((target.getTime() - start) / 86_400_000);
}
