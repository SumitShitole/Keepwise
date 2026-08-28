import { StatusBar } from "expo-status-bar";
import { useState } from "react";
import { ActivityIndicator, Pressable, SafeAreaView, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";

const API = process.env.EXPO_PUBLIC_API_URL ?? "http://127.0.0.1:43124";

export default function App() {
  const [email, setEmail] = useState("sumit@keepwise.app");
  const [token, setToken] = useState<string | null>(null);
  const [summary, setSummary] = useState<string>("Sign in to see your dashboard.");
  const [busy, setBusy] = useState(false);

  async function signIn() {
    setBusy(true);
    try {
      const auth = await fetch(`${API}/v1/auth/dev-login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, displayName: "Sumit" })
      }).then((r) => r.json());
      setToken(auth.accessToken);
      const dash = await fetch(`${API}/v1/dashboard`, {
        headers: { Authorization: `Bearer ${auth.accessToken}` }
      }).then((r) => r.json());
      setSummary(
        `${dash.totalItems} items · ${dash.activeWarranties} active warranties · ${dash.warrantiesExpiringSoon} expiring soon`
      );
    } catch {
      setSummary("Could not reach the Keepwise API. Start the backend on port 43124.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.container}>
        <Text style={styles.brand}>Keepwise</Text>
        <Text style={styles.title}>Warranty and maintenance reminders</Text>
        <Text style={styles.body}>{summary}</Text>
        {!token ? (
          <View style={styles.card}>
            <TextInput style={styles.input} value={email} onChangeText={setEmail} autoCapitalize="none" />
            <Pressable style={styles.button} onPress={signIn} disabled={busy}>
              {busy ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Continue</Text>}
            </Pressable>
          </View>
        ) : null}
        <StatusBar style="dark" />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: "#f4f7f4" },
  container: { padding: 24, gap: 12 },
  brand: { color: "#1f7a4d", fontWeight: "700", fontSize: 16, textTransform: "uppercase" },
  title: { fontSize: 28, fontWeight: "700", color: "#163027" },
  body: { fontSize: 16, color: "#3f4f47" },
  card: { backgroundColor: "#fff", padding: 16, borderRadius: 12, gap: 12, marginTop: 12 },
  input: { borderWidth: 1, borderColor: "#d4d4d8", borderRadius: 8, padding: 12 },
  button: { backgroundColor: "#1f7a4d", borderRadius: 8, padding: 14, alignItems: "center" },
  buttonText: { color: "#fff", fontWeight: "600" }
});
