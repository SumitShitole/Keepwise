import { useEffect, useState } from "react";
import { Switch, Text, View } from "react-native";
import type { IngestionSettings, UserProfile } from "@keepwise/shared";
import { api, setToken } from "../api";
import { Card, ErrorText, Field, PrimaryButton, ScreenTitle, styles } from "../ui";

export function SettingsScreen({ onSignedOut }: { onSignedOut: () => void }) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [ingestion, setIngestion] = useState<IngestionSettings | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.me().then(setProfile).catch((err: Error) => setError(err.message));
    api.ingestionSettings().then(setIngestion).catch(() => undefined);
  }, []);

  if (error && !profile) {
    return <ErrorText message={error} />;
  }
  if (!profile) {
    return <Text style={styles.body}>Loading profile…</Text>;
  }

  async function saveProfile() {
    if (!profile) {
      return;
    }
    const updated = await api.updateMe({
      displayName: profile.displayName,
      mobileNumber: profile.mobileNumber,
      countryCode: profile.countryCode,
      timeZoneId: profile.timeZoneId,
      language: "en",
      pushEnabled: profile.pushEnabled,
      emailEnabled: profile.emailEnabled,
      smsEnabled: false,
      whatsAppEnabled: false
    });
    setProfile(updated);
    setMessage("Saved.");
  }

  async function savePrivacy() {
    if (!ingestion) {
      return;
    }
    const updated = await api.updateIngestionSettings({
      ...ingestion,
      emailScanningEnabled: false,
      smsImportEnabled: false,
      whatsAppImportEnabled: false
    });
    setIngestion(updated);
    setMessage("Saved.");
  }

  return (
    <View style={{ gap: 12 }}>
      <ScreenTitle title="Settings" />
      <Card>
        <Field label="Name" value={profile.displayName} onChangeText={(displayName) => setProfile({ ...profile, displayName })} />
        <Field label="Email" value={profile.email} onChangeText={() => undefined} editable={false} />
        <Field
          label="Mobile"
          value={profile.mobileNumber ?? ""}
          onChangeText={(mobileNumber) => setProfile({ ...profile, mobileNumber: mobileNumber || null })}
        />
        <Field label="Country" value={profile.countryCode} onChangeText={(countryCode) => setProfile({ ...profile, countryCode })} />
        <Field label="Timezone" value={profile.timeZoneId} onChangeText={(timeZoneId) => setProfile({ ...profile, timeZoneId })} />
        <View style={styles.row}>
          <Text style={styles.body}>Push notifications</Text>
          <Switch value={profile.pushEnabled} onValueChange={(pushEnabled) => setProfile({ ...profile, pushEnabled })} />
        </View>
        <View style={styles.row}>
          <Text style={styles.body}>Email notifications</Text>
          <Switch value={profile.emailEnabled} onValueChange={(emailEnabled) => setProfile({ ...profile, emailEnabled })} />
        </View>
        {message ? <Text style={styles.back}>{message}</Text> : null}
        <PrimaryButton label="Save" onPress={() => void saveProfile()} />
        <PrimaryButton
          label="Sign out"
          onPress={() => {
            setToken(null);
            onSignedOut();
          }}
        />
      </Card>
      {ingestion ? (
        <Card>
          <Text style={styles.body}>Privacy and detection</Text>
          <Text style={styles.muted}>
            Keepwise never reads your SMS or WhatsApp inbox. AI is off until you enable it. Imported text is treated as data, not
            instructions.
          </Text>
          <View style={styles.row}>
            <Text style={styles.body}>Receipt / PDF scanning</Text>
            <Switch
              value={ingestion.receiptScanningEnabled}
              onValueChange={(receiptScanningEnabled) => setIngestion({ ...ingestion, receiptScanningEnabled })}
            />
          </View>
          <View style={styles.row}>
            <Text style={styles.body}>Shared text import</Text>
            <Switch
              value={ingestion.sharedTextEnabled}
              onValueChange={(sharedTextEnabled) => setIngestion({ ...ingestion, sharedTextEnabled })}
            />
          </View>
          <View style={styles.row}>
            <Text style={styles.body}>Allow AI extraction</Text>
            <Switch
              value={ingestion.aiProcessingEnabled}
              onValueChange={(aiProcessingEnabled) => setIngestion({ ...ingestion, aiProcessingEnabled })}
            />
          </View>
          <PrimaryButton label="Save privacy" onPress={() => void savePrivacy()} />
        </Card>
      ) : null}
    </View>
  );
}
