import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import type { Category } from "@keepwise/shared";
import { api } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle, styles } from "../ui";

export function NewItemScreen({ onBack, onCreated }: { onBack: () => void; onCreated: (id: string) => void }) {
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoryId, setCategoryId] = useState("");
  const [name, setName] = useState("");
  const [brand, setBrand] = useState("");
  const [modelNumber, setModelNumber] = useState("");
  const [purchaseDate, setPurchaseDate] = useState("");
  const [purchasePrice, setPurchasePrice] = useState("");
  const [vendorName, setVendorName] = useState("");
  const [warrantyYears, setWarrantyYears] = useState("2");
  const [explicitExpiry, setExplicitExpiry] = useState("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    api.categories().then(setCategories).catch((err: Error) => setError(err.message));
  }, []);

  async function save() {
    setLoading(true);
    setError(null);
    try {
      const durationYears = Number(warrantyYears || 0);
      if (explicitExpiry && !purchaseDate) {
        setError("Add a purchase date on or before the warranty expiry.");
        setLoading(false);
        return;
      }
      if (explicitExpiry && purchaseDate && explicitExpiry < purchaseDate) {
        setError("Warranty expiry cannot be earlier than the start date.");
        setLoading(false);
        return;
      }
      const created = await api.createItem({
        name,
        categoryId: categoryId || null,
        brand: brand || null,
        modelNumber: modelNumber || null,
        purchaseDate: purchaseDate || null,
        purchasePrice: purchasePrice ? Number(purchasePrice) : null,
        currency: "INR",
        vendorName: vendorName || null,
        notes: notes || null,
        warranty:
          durationYears || explicitExpiry
            ? {
                kind: 0,
                startDate: purchaseDate || null,
                durationValue: explicitExpiry ? null : durationYears || 1,
                durationUnit: explicitExpiry ? null : 3,
                explicitEndDate: explicitExpiry || null
              }
            : null
      });
      onCreated(created.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save item.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={{ gap: 12 }}>
      <Pressable onPress={onBack} accessibilityRole="button">
        <Text style={styles.back}>Back</Text>
      </Pressable>
      <ScreenTitle title="Add item" />
      <Card>
        <Field label="Name" value={name} onChangeText={setName} placeholder="Samsung washing machine" />
        <Text style={styles.label}>Category</Text>
        {categories.map((category) => (
          <Pressable
            key={category.id}
            onPress={() => setCategoryId(category.id)}
            accessibilityRole="button"
            accessibilityState={{ selected: categoryId === category.id }}
          >
            <Text style={categoryId === category.id ? styles.back : styles.muted}>{category.name}</Text>
          </Pressable>
        ))}
        <Field label="Brand" value={brand} onChangeText={setBrand} />
        <Field label="Model" value={modelNumber} onChangeText={setModelNumber} />
        <Field label="Purchase date (YYYY-MM-DD)" value={purchaseDate} onChangeText={setPurchaseDate} />
        <Field label="Price (INR)" value={purchasePrice} onChangeText={setPurchasePrice} keyboardType="numeric" />
        <Field label="Vendor" value={vendorName} onChangeText={setVendorName} />
        <Field label="Warranty years" value={warrantyYears} onChangeText={setWarrantyYears} keyboardType="numeric" />
        <Field label="Or explicit expiry (YYYY-MM-DD)" value={explicitExpiry} onChangeText={setExplicitExpiry} />
        <Field label="Notes" value={notes} onChangeText={setNotes} />
        <ErrorText message={error} />
        <PrimaryButton label={loading ? "Saving…" : "Save item"} onPress={() => void save()} disabled={loading || !name.trim()} />
      </Card>
    </View>
  );
}
