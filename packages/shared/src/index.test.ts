import { describe, expect, it } from "vitest";
import { daysUntil } from "./index";

describe("daysUntil", () => {
  it("is zero for today", () => {
    expect(daysUntil("2027-03-15", new Date("2027-03-15T12:00:00Z"))).toBe(0);
  });
});
