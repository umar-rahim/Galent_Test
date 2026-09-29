import type { FormData } from "../../types";

export const filingStatuses = [
  ["Single", "Single"],
  ["MarriedFilingJointly", "Married filing jointly"],
  ["MarriedFilingSeparately", "Married filing separately"],
  ["HeadOfHousehold", "Head of household"],
  ["QualifyingSurvivingSpouse", "Qualifying surviving spouse"],
];
export const incomeLines = ["1a", "1b", "1c", "1d", "1e", "1f", "1g", "1h", "1i", "2a", "2b", "3a", "3b", "4a", "4b", "5a", "5b", "6a", "6b", "7a", "8"];
export const taxLines = ["10", "11b", "12", "13a", "13b", "16", "17", "18", "19", "20", "21", "22", "23"];
export const paymentLines = ["25a", "25b", "25c", "26", "27", "28", "29", "30", "31", "32", "35a", "36", "38"];
export const computedLines = ["1z", "9", "11a", "14", "15", "24", "25d", "33", "34", "37"];

export const emptyForm = (): FormData => ({
  taxpayerFirstName: "", taxpayerLastName: "", taxpayerSsn: "", spouseFirstName: "", spouseLastName: "", spouseSsn: "",
  addressLine1: "", addressLine2: "", city: "", state: "", zipCode: "", foreignCountry: "", foreignProvince: "", foreignPostalCode: "",
  filingStatus: "", digitalAssetsQuestionYes: false, moreThanFourDependents: false, dependents: [],
  thirdPartyDesignee: false, designeeName: "", designeePhone: "", designeePin: "", routingNumber: "", bankAccountType: "", accountNumber: "",
});

export function standardDeduction(status: string): number {
  if (status === "MarriedFilingJointly" || status === "QualifyingSurvivingSpouse") return 31500;
  if (status === "HeadOfHousehold") return 23625;
  return 15750;
}

export function formatSsn(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 9);
  if (digits.length <= 3) return digits;
  if (digits.length <= 5) return `${digits.slice(0, 3)}-${digits.slice(3)}`;
  return `${digits.slice(0, 3)}-${digits.slice(3, 5)}-${digits.slice(5)}`;
}

export function calculate(data: FormData): FormData {
  const amount = (line: string) => Number(data[`line${line}`] ?? 0) || 0;
  const sum = (...lines: string[]) => lines.reduce((total, line) => total + amount(line), 0);
  const line1z = sum("1a", "1b", "1c", "1d", "1e", "1f", "1g", "1h");
  const line9 = line1z + sum("2b", "3b", "4b", "5b", "6b", "7a", "8");
  const line11a = line9 - amount("10");
  const line12 = data.line12 === undefined || data.line12 === null || data.line12 === ""
    ? standardDeduction(data.filingStatus)
    : amount("12");
  const line14 = line12 + amount("13a") + amount("13b");
  const line15 = Math.max(0, line11a - line14);
  const line24 = amount("22") + amount("23");
  const line25d = amount("25a") + amount("25b") + amount("25c");
  const line33 = line25d + amount("26") + amount("32");
  const line34 = Math.max(0, line33 - line24);
  const line37 = Math.max(0, line24 - line33);
  return {
    ...data,
    line1z,
    line9,
    line11a,
    line12,
    line14,
    line15,
    line24,
    line25d,
    line33,
    line34,
    line35a: Math.max(0, line34 - amount("36")),
    line37,
  };
}