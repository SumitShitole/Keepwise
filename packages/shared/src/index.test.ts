import { describe, expect, it } from "vitest";
import { apiErrorMessage, daysUntil } from "./index";

describe("apiErrorMessage", () => {
  it("reads the Keepwise error envelope", () => {
    expect(
      apiErrorMessage(
        '{"error":{"code":"expiry_before_start","message":"Warranty expiry cannot be earlier than the start date.","errors":null}}'
      )
    ).toBe("Warranty expiry cannot be earlier than the start date.");
  });
});

describe("daysUntil", () => {
  it("is zero for today", () => {
    expect(daysUntil("2027-03-15", new Date("2027-03-15T12:00:00Z"))).toBe(0);
  });
});
