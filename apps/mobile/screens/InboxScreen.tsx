import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import type { PurchaseCandidate } from "@keepwise/shared";
import { api } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle, styles } from "../ui";

const statusLabel: Record<number, string> = {
  0: "Processing",
  1: "Needs review",
  2: "Confirmed",
  3: "Ignored",
  4: "Failed",
  5: "Duplicate",
  6: "Needs OCR"
};

export function InboxScreen({ onCandidate }: { onCandidate: (id: string) => void }) {
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

  async function extract() {
    setBusy(true);
    setError(null);
    try {
      const result = await api.ingestText(text);
      await load();
      if (result.candidateId) {
        onCandidate(result.candidateId);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Import failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <View style={{ gap: 12 }}>
      <ScreenTitle
        title="Purchase inbox"
        subtitle="Paste an order SMS/email. Keepwise extracts a candidate — you confirm before anything is saved as an asset."
      />
      <Card>
        <Field label="Shared order text" value={text} onChangeText={setText} multiline placeholder="Amazon.in order confirmation..." />
        <PrimaryButton label={busy ? "Extracting…" : "Extract from text"} onPress={() => void extract()} disabled={busy || !text.trim()} />
        <Text style={styles.muted}>Receipt PDF/photo upload is available on the web app.</Text>
      </Card>
      <ErrorText message={error} />
      {rows.length === 0 ? (
        <Card>
          <Text style={styles.muted}>No imported purchases yet.</Text>
        </Card>
      ) : (
        rows.map((row) => (
          <Pressable key={row.id} onPress={() => onCandidate(row.id)} accessibilityRole="button">
            <Card>
              <View style={styles.row}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.body}>{row.payload.productName ?? "Untitled purchase"}</Text>
                  <Text style={styles.muted}>
                    {[row.payload.vendor, row.payload.purchaseDate, row.payload.amount != null ? `₹${row.payload.amount}` : null]
                      .filter(Boolean)
                      .join(" · ")}
                  </Text>
                </View>
                <Text style={styles.chip}>{statusLabel[row.status]}</Text>
              </View>
            </Card>
          </Pressable>
        ))
      )}
    </View>
  );
}
