import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import { coverageKindLabel, type ItemDetail } from "@keepwise/shared";
import { api } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle, StatusText, styles } from "../ui";

export function ItemDetailScreen({ id, onBack, onDeleted }: { id: string; onBack: () => void; onDeleted: () => void }) {
  const [item, setItem] = useState<ItemDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [startDate, setStartDate] = useState("");
  const [months, setMonths] = useState("6");

  async function load() {
    try {
      setItem(await api.item(id));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Not found");
    }
  }

  useEffect(() => {
    void load();
  }, [id]);

  if (error && !item) {
    return (
      <View style={{ gap: 12 }}>
        <Pressable onPress={onBack} accessibilityRole="button">
          <Text style={styles.back}>Back</Text>
        </Pressable>
        <ErrorText message={error} />
      </View>
    );
  }

  if (!item) {
    return <Text style={styles.body}>Loading item…</Text>;
  }

  return (
    <View style={{ gap: 12 }}>
      <Pressable onPress={onBack} accessibilityRole="button">
        <Text style={styles.back}>Back</Text>
      </Pressable>
      <View style={styles.row}>
        <ScreenTitle title={item.name} subtitle={[item.brand, item.modelNumber, item.categoryName].filter(Boolean).join(" · ")} />
      </View>
      <PrimaryButton label="Delete" danger onPress={() => void api.deleteItem(id).then(onDeleted)} />
      <Card>
        <Text style={styles.body}>Purchase</Text>
        <Text style={styles.muted}>Date: {item.purchaseDate ?? "—"}</Text>
        <Text style={styles.muted}>Price: {item.purchasePrice != null ? `${item.currency} ${item.purchasePrice}` : "—"}</Text>
        <Text style={styles.muted}>Vendor: {item.vendorName ?? "—"}</Text>
        <Text style={styles.muted}>{item.notes ?? "No notes"}</Text>
      </Card>
      <Card>
        <Text style={styles.body}>Coverages</Text>
        {item.coverages.length === 0 ? (
          <Text style={styles.muted}>No warranty or maintenance yet.</Text>
        ) : (
          item.coverages.map((coverage) => (
            <View key={coverage.id} style={{ gap: 4 }}>
              <View style={styles.row}>
                <Text style={styles.body}>{coverageKindLabel[coverage.kind]}</Text>
                <StatusText status={coverage.status} />
              </View>
              <Text style={styles.muted}>
                {coverage.startDate} → {coverage.kind === 1 ? coverage.nextDueDate : coverage.endDate}
              </Text>
              {coverage.kind === 1 ? (
                <Pressable
                  accessibilityRole="button"
                  onPress={() =>
                    void api.completeMaintenance(coverage.id, new Date().toISOString().slice(0, 10)).then(() => load())
                  }
                >
                  <Text style={styles.back}>Mark maintenance complete</Text>
                </Pressable>
              ) : null}
            </View>
          ))
        )}
        <Field label="Maintenance start (YYYY-MM-DD)" value={startDate} onChangeText={setStartDate} />
        <Field label="Every N months" value={months} onChangeText={setMonths} keyboardType="numeric" />
        <PrimaryButton
          label="Add recurring maintenance"
          onPress={() =>
            void api
              .addCoverage(id, { kind: 1, startDate, recurrenceValue: Number(months || 6), recurrenceUnit: 2 })
              .then(() => load())
              .catch((err: Error) => setError(err.message))
          }
        />
        <ErrorText message={error} />
      </Card>
    </View>
  );
}
