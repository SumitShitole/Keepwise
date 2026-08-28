import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import type { ReactNode } from "react";
import { coverageStatusLabel } from "@keepwise/shared";

export const colors = {
  bg: "#f4f7f4",
  brand: "#1f7a4d",
  title: "#163027",
  body: "#3f4f47",
  muted: "#6b7c74",
  card: "#fff",
  border: "#d4d4d8",
  danger: "#b91c1c",
  error: "#9f1239"
};

export function Card({ children }: { children: ReactNode }) {
  return <View style={styles.card}>{children}</View>;
}

export function Field({
  label,
  value,
  onChangeText,
  placeholder,
  keyboardType,
  multiline,
  editable = true
}: {
  label: string;
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
  keyboardType?: "default" | "numeric" | "email-address";
  multiline?: boolean;
  editable?: boolean;
}) {
  return (
    <View style={styles.field}>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        style={[styles.input, multiline ? styles.multiline : null, !editable ? styles.disabled : null]}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        keyboardType={keyboardType}
        multiline={multiline}
        editable={editable}
        autoCapitalize={keyboardType === "email-address" ? "none" : "sentences"}
        accessibilityLabel={label}
      />
    </View>
  );
}

export function PrimaryButton({
  label,
  onPress,
  disabled,
  danger
}: {
  label: string;
  onPress: () => void;
  disabled?: boolean;
  danger?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      onPress={onPress}
      disabled={disabled}
      style={[styles.button, danger ? styles.danger : null, disabled ? styles.buttonDisabled : null]}
    >
      <Text style={styles.buttonText}>{label}</Text>
    </Pressable>
  );
}

export function StatusText({ status }: { status: number | null }) {
  if (status === null || status === undefined) {
    return <Text style={styles.muted}>No warranty</Text>;
  }
  return <Text style={styles.chip}>{coverageStatusLabel[status] ?? "Status"}</Text>;
}

export function ScreenTitle({ title, subtitle }: { title: string; subtitle?: string }) {
  return (
    <View style={styles.titleBlock}>
      <Text style={styles.title}>{title}</Text>
      {subtitle ? <Text style={styles.body}>{subtitle}</Text> : null}
    </View>
  );
}

export function ErrorText({ message }: { message: string | null }) {
  if (!message) {
    return null;
  }
  return <Text style={styles.error}>{message}</Text>;
}

export const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.bg, paddingTop: 48 },
  bodyScroll: { padding: 20, paddingBottom: 32, gap: 12 },
  brand: { color: colors.brand, fontWeight: "700", fontSize: 16, textTransform: "uppercase" },
  titleBlock: { gap: 6, marginBottom: 4 },
  title: { fontSize: 26, fontWeight: "700", color: colors.title },
  body: { fontSize: 16, color: colors.body },
  muted: { fontSize: 14, color: colors.muted },
  error: { fontSize: 14, color: colors.error },
  card: { backgroundColor: colors.card, padding: 16, borderRadius: 12, gap: 8, borderWidth: 1, borderColor: colors.border },
  field: { gap: 6 },
  label: { fontSize: 14, color: colors.body },
  input: { borderWidth: 1, borderColor: colors.border, borderRadius: 8, padding: 12, backgroundColor: "#fff" },
  multiline: { minHeight: 96, textAlignVertical: "top" },
  disabled: { backgroundColor: "#f4f4f5", color: colors.muted },
  button: { backgroundColor: colors.brand, borderRadius: 8, padding: 14, alignItems: "center" },
  danger: { backgroundColor: colors.danger },
  buttonDisabled: { opacity: 0.5 },
  buttonText: { color: "#fff", fontWeight: "600" },
  chip: { fontSize: 12, fontWeight: "600", color: colors.brand },
  row: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", gap: 8 },
  tabBar: {
    flexDirection: "row",
    borderTopWidth: 1,
    borderTopColor: colors.border,
    backgroundColor: "#fff",
    paddingBottom: 18,
    paddingTop: 8
  },
  tab: { flex: 1, alignItems: "center", paddingVertical: 8 },
  tabLabel: { fontSize: 12, color: colors.muted, fontWeight: "600" },
  tabActive: { color: colors.brand },
  back: { color: colors.brand, fontWeight: "600", marginBottom: 8 }
});
