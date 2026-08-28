import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import type { PurchaseCandidate } from "@keepwise/shared";
import { api } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle, styles } from "../ui";

export function CandidateScreen({
  id,
  onBack,
  onConfirmed,
  onIgnored
}: {
  id: string;
  onBack: () => void;
  onConfirmed: (itemId: string) => void;
  onIgnored: () => void;
}) {
  const [row, setRow] = useState<PurchaseCandidate | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [productName, setProductName] = useState("");
  const [vendor, setVendor] = useState("");
  const [brand, setBrand] = useState("");
  const [purchaseDate, setPurchaseDate] = useState("");
  const [amount, setAmount] = useState("");
  const [orderNumber, setOrderNumber] = useState("");
  const [warrantyMonths, setWarrantyMonths] = useState("");
  const [returnDays, setReturnDays] = useState("");

  useEffect(() => {
    api
      .candidate(id)
      .then((candidate) => {
        setRow(candidate);
        const p = candidate.payload;
        setProductName(p.productName ?? "");
        setVendor(p.vendor ?? "");
        setBrand(p.brand ?? "");
        setPurchaseDate(p.purchaseDate ?? "");
        setAmount(p.amount != null ? String(p.amount) : "");
        setOrderNumber(p.orderNumber ?? "");
        setWarrantyMonths(p.warrantyDurationMonths != null ? String(p.warrantyDurationMonths) : "");
        setReturnDays(p.returnWindowDays != null ? String(p.returnWindowDays) : "");
      })
      .catch((err: Error) => setLoadError(err.message));
  }, [id]);

  if (loadError) {
    return (
      <View style={{ gap: 12 }}>
        <Pressable onPress={onBack} accessibilityRole="button">
          <Text style={styles.back}>Back</Text>
        </Pressable>
        <ErrorText message={loadError} />
      </View>
    );
  }

  if (!row) {
    return <Text style={styles.body}>Loading candidate…</Text>;
  }

  const editable = row.status === 1 || row.status === 5 || row.status === 6 || row.status === 4;
  const p = row.payload;

  async function save() {
    if (!row) {
      return;
    }
    try {
      setError(null);
      const updated = await api.editCandidate(row.id, {
        ...row.payload,
        productName,
        vendor: vendor || null,
        brand: brand || null,
        purchaseDate: purchaseDate || null,
        amount: amount ? Number(amount) : null,
        orderNumber: orderNumber || null,
        warrantyDurationMonths: warrantyMonths ? Number(warrantyMonths) : null,
        returnWindowDays: returnDays ? Number(returnDays) : null
      });
      setRow(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save edits.");
    }
  }

  return (
    <View style={{ gap: 12 }}>
      <Pressable onPress={onBack} accessibilityRole="button">
        <Text style={styles.back}>Back</Text>
      </Pressable>
      <ScreenTitle
        title="Review purchase"
        subtitle={`Confidence ${Math.round(row.overallConfidence * 100)}%.${p.warrantyProvenance === 4 ? " Warranty is not confirmed from the document." : ""}`}
      />
      <Card>
        <Field label="Product" value={productName} onChangeText={setProductName} />
        <Field label="Vendor" value={vendor} onChangeText={setVendor} />
        <Field label="Brand" value={brand} onChangeText={setBrand} />
        <Field label="Purchase date (YYYY-MM-DD)" value={purchaseDate} onChangeText={setPurchaseDate} />
        <Field label="Amount" value={amount} onChangeText={setAmount} keyboardType="numeric" />
        <Field label="Order number" value={orderNumber} onChangeText={setOrderNumber} />
        <Field label="Warranty months" value={warrantyMonths} onChangeText={setWarrantyMonths} keyboardType="numeric" />
        <Field label="Return window days" value={returnDays} onChangeText={setReturnDays} keyboardType="numeric" />
        <ErrorText message={error} />
        <PrimaryButton label="Save edits" onPress={() => void save()} disabled={!editable} />
        <PrimaryButton
          label="Confirm"
          onPress={() =>
            void api
              .confirmCandidate(row.id)
              .then((result) => onConfirmed(result.itemId))
              .catch((err: Error) => setError(err.message))
          }
          disabled={!editable}
        />
        <PrimaryButton
          label="Ignore"
          danger
          onPress={() => void api.ignoreCandidate(row.id).then(onIgnored)}
          disabled={!editable}
        />
      </Card>
    </View>
  );
}
