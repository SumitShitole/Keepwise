import { test, expect } from "@playwright/test";

test("welcome page renders", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: /never miss a warranty/i })).toBeVisible();
});
