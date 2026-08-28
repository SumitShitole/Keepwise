import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import type { ItemSummary } from "@keepwise/shared";
import { api } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle, StatusText, styles } from "../ui";

export function ItemsScreen({ onItem, onAddItem }: { onItem: (id: string) => void; onAddItem: () => void }) {
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<ItemSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handle = setTimeout(() => {
      const query = search ? `?search=${encodeURIComponent(search)}` : "";
      api
        .items(query)
        .then((result) => {
          setItems(result.items);
          setTotal(result.total);
        })
        .catch((err: Error) => setError(err.message));
    }, 200);
    return () => clearTimeout(handle);
  }, [search]);

  return (
    <View style={{ gap: 12 }}>
      <View style={styles.row}>
        <ScreenTitle title="Items" />
        <PrimaryButton label="Add item" onPress={onAddItem} />
      </View>
      <Field label="Search" value={search} onChangeText={setSearch} placeholder="Name, brand, model, vendor…" />
      <ErrorText message={error} />
      <Text style={styles.muted}>{total} items</Text>
      {items.length === 0 ? (
        <Card>
          <Text style={styles.muted}>No items match this search. Add a purchase to start tracking dates.</Text>
        </Card>
      ) : (
        items.map((item) => (
          <Pressable key={item.id} onPress={() => onItem(item.id)} accessibilityRole="button" accessibilityLabel={item.name}>
            <Card>
              <View style={styles.row}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.body}>{item.name}</Text>
                  <Text style={styles.muted}>{[item.brand, item.categoryName].filter(Boolean).join(" · ") || "Uncategorized"}</Text>
                </View>
                <StatusText status={item.warrantyStatus} />
              </View>
              {item.warrantyEndDate ? <Text style={styles.muted}>Warranty until {item.warrantyEndDate}</Text> : null}
            </Card>
          </Pressable>
        ))
      )}
    </View>
  );
}
