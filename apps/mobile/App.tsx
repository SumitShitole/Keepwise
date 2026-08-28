import { StatusBar } from "expo-status-bar";
import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { CandidateScreen } from "./screens/CandidateScreen";
import { DashboardScreen } from "./screens/DashboardScreen";
import { InboxScreen } from "./screens/InboxScreen";
import { ItemDetailScreen } from "./screens/ItemDetailScreen";
import { ItemsScreen } from "./screens/ItemsScreen";
import { NewItemScreen } from "./screens/NewItemScreen";
import { SettingsScreen } from "./screens/SettingsScreen";
import { SignInScreen } from "./screens/SignInScreen";
import { styles } from "./ui";

type TabId = "dashboard" | "inbox" | "items" | "settings";
type Screen =
  | { kind: "signin" }
  | { kind: "main"; tab: TabId }
  | { kind: "item"; id: string }
  | { kind: "new-item" }
  | { kind: "candidate"; id: string };

const TABS: { id: TabId; label: string }[] = [
  { id: "dashboard", label: "Dashboard" },
  { id: "inbox", label: "Inbox" },
  { id: "items", label: "Items" },
  { id: "settings", label: "Settings" }
];

export default function App() {
  const [screen, setScreen] = useState<Screen>({ kind: "signin" });

  const showTabs = screen.kind === "main";

  return (
    <View style={styles.safe}>
      <ScrollView contentContainerStyle={styles.bodyScroll} keyboardShouldPersistTaps="handled">
        <Text style={styles.brand}>Keepwise</Text>
        {screen.kind === "signin" ? <SignInScreen onSignedIn={() => setScreen({ kind: "main", tab: "dashboard" })} /> : null}
        {screen.kind === "main" && screen.tab === "dashboard" ? (
          <DashboardScreen
            onItem={(id) => setScreen({ kind: "item", id })}
            onCandidate={(id) => setScreen({ kind: "candidate", id })}
            onAddItem={() => setScreen({ kind: "new-item" })}
          />
        ) : null}
        {screen.kind === "main" && screen.tab === "inbox" ? (
          <InboxScreen onCandidate={(id) => setScreen({ kind: "candidate", id })} />
        ) : null}
        {screen.kind === "main" && screen.tab === "items" ? (
          <ItemsScreen onItem={(id) => setScreen({ kind: "item", id })} onAddItem={() => setScreen({ kind: "new-item" })} />
        ) : null}
        {screen.kind === "main" && screen.tab === "settings" ? (
          <SettingsScreen onSignedOut={() => setScreen({ kind: "signin" })} />
        ) : null}
        {screen.kind === "item" ? (
          <ItemDetailScreen
            id={screen.id}
            onBack={() => setScreen({ kind: "main", tab: "items" })}
            onDeleted={() => setScreen({ kind: "main", tab: "items" })}
          />
        ) : null}
        {screen.kind === "new-item" ? (
          <NewItemScreen
            onBack={() => setScreen({ kind: "main", tab: "items" })}
            onCreated={(id) => setScreen({ kind: "item", id })}
          />
        ) : null}
        {screen.kind === "candidate" ? (
          <CandidateScreen
            id={screen.id}
            onBack={() => setScreen({ kind: "main", tab: "inbox" })}
            onConfirmed={(itemId) => setScreen({ kind: "item", id: itemId })}
            onIgnored={() => setScreen({ kind: "main", tab: "inbox" })}
          />
        ) : null}
        <StatusBar style="dark" />
      </ScrollView>
      {showTabs ? (
        <View style={styles.tabBar} accessibilityRole="tablist">
          {TABS.map((tab) => {
            const active = screen.kind === "main" && screen.tab === tab.id;
            return (
              <Pressable
                key={tab.id}
                style={styles.tab}
                onPress={() => setScreen({ kind: "main", tab: tab.id })}
                accessibilityRole="tab"
                accessibilityState={{ selected: active }}
                accessibilityLabel={tab.label}
              >
                <Text style={[styles.tabLabel, active ? styles.tabActive : null]}>{tab.label}</Text>
              </Pressable>
            );
          })}
        </View>
      ) : null}
    </View>
  );
}
