import { useEffect, useState } from "react";
import { Pressable, Text, View } from "react-native";
import { coverageKindLabel, type Dashboard } from "@keepwise/shared";
import { api } from "../api";
import { Card, ErrorText, ScreenTitle, StatusText, styles } from "../ui";

export function DashboardScreen({
  onItem,
  onCandidate,
  onAddItem
}: {
  onItem: (id: string) => void;
  onCandidate: (id: string) => void;
  onAddItem: () => void;
}) {
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .dashboard()
      .then(setData)
      .catch((err: Error) => setError(err.message));
  }, []);

  if (error) {
    return <ErrorText message={error} />;
  }
  if (!data) {
    return <Text style={styles.body}>Loading your reminders…</Text>;
  }

  const stats: [string, number][] = [
    ["Items", data.totalItems],
    ["Active warranties", data.activeWarranties],
    ["Expiring soon", data.warrantiesExpiringSoon],
    ["Maintenance due", data.upcomingMaintenance],
    ["Renewals due", data.upcomingRenewals],
    ["Expired", data.expiredItems]
  ];

  function openHref(href: string | null) {
    if (!href) {
      return;
    }
    const item = href.match(/^\/items\/([^/]+)/);
    const inbox = href.match(/^\/inbox\/([^/]+)/);
    if (item) {
      onItem(item[1]);
    } else if (inbox) {
      onCandidate(inbox[1]);
    }
  }

  return (
    <View style={{ gap: 12 }}>
      <ScreenTitle title="What needs your attention" />
      {data.attention.length === 0 ? (
        <Text style={styles.muted}>Nothing urgent. Your coverages look calm.</Text>
      ) : (
        <Card>
          {data.attention.map((item) => (
            <Pressable key={item.title} onPress={() => openHref(item.href)} accessibilityRole="button">
              <Text style={styles.body}>{item.title}</Text>
              <Text style={styles.muted}>{item.detail}</Text>
              {item.href ? <Text style={styles.back}>Review</Text> : null}
            </Pressable>
          ))}
        </Card>
      )}
      {stats.map(([label, value]) => (
        <Card key={label}>
          <Text style={styles.muted}>{label}</Text>
          <Text style={styles.title}>{value}</Text>
        </Card>
      ))}
      <Card>
        <Text style={styles.body}>Upcoming</Text>
        {data.upcomingEvents.length === 0 ? (
          <Text style={styles.muted}>Nothing due in the next 30 days.</Text>
        ) : (
          data.upcomingEvents.map((event) => (
            <Pressable key={event.coverageId} onPress={() => onItem(event.itemId)} style={styles.row} accessibilityRole="button">
              <Text style={styles.body}>{event.itemName}</Text>
              <Text style={styles.muted}>
                {coverageKindLabel[event.kind]} · {event.date}
              </Text>
            </Pressable>
          ))
        )}
      </Card>
      <Card>
        <Text style={styles.body}>Recently added</Text>
        {data.recentlyAdded.length === 0 ? (
          <Pressable onPress={onAddItem} accessibilityRole="button">
            <Text style={styles.muted}>No items yet. Add your first item</Text>
          </Pressable>
        ) : (
          data.recentlyAdded.map((item) => (
            <Pressable key={item.id} onPress={() => onItem(item.id)} accessibilityRole="button">
              <Text style={styles.body}>{item.name}</Text>
            </Pressable>
          ))
        )}
      </Card>
    </View>
  );
}
