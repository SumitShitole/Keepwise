import { useState } from "react";
import { ActivityIndicator, View } from "react-native";
import { api, setToken } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle } from "../ui";

export function SignInScreen({ onSignedIn }: { onSignedIn: () => void }) {
  const [email, setEmail] = useState("sumit@keepwise.app");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function signIn() {
    setBusy(true);
    setError(null);
    try {
      const auth = await api.devLogin(email, "Sumit");
      setToken(auth.accessToken);
      onSignedIn();
    } catch {
      setError("Could not reach the Keepwise API. Start the backend on port 43124.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <View style={{ gap: 12 }}>
      <ScreenTitle title="Warranty and maintenance reminders" subtitle="Sign in to see your dashboard." />
      <Card>
        <Field label="Email" value={email} onChangeText={setEmail} keyboardType="email-address" />
        {busy ? <ActivityIndicator color="#1f7a4d" /> : <PrimaryButton label="Continue" onPress={() => void signIn()} />}
        <ErrorText message={error} />
      </Card>
    </View>
  );
}
