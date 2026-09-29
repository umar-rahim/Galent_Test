import type { Finding, FormData } from "../types";
import { computedLines } from "../features/form1040/form1040";
import { money } from "../utils/money";

export function FieldErrors({ errors }: { errors: Finding[] }) {
  if (!errors.length) return null;
  return <span className="field-error" role="alert">{errors[0].message}</span>;
}

export function TextField({ label, name, value, update, errors = [], placeholder, inputMode, required = false }: {
  label: string; name: string; value: unknown; update: (field: string, value: unknown) => void; errors?: Finding[]; placeholder?: string; inputMode?: "text" | "numeric"; required?: boolean;
}) {
  return <label className="field-label">{label}<input required={required} value={String(value ?? "")} placeholder={placeholder} inputMode={inputMode} autoComplete="off" onChange={(event) => update(name, event.target.value)} /><FieldErrors errors={errors} /></label>;
}

export function DateField({ label, name, value, update }: { label: string; name: string; value: unknown; update: (field: string, value: unknown) => void }) {
  return <label className="field-label">{label}<input type="date" value={String(value ?? "")} onChange={(event) => update(name, event.target.value || null)} /></label>;
}

export function MoneyField({ line, value, update, errors = [] }: { line: string; value: unknown; update: (field: string, value: unknown) => void; errors?: Finding[] }) {
  const isComputed = computedLines.includes(line);
  return <label className={`money-field ${isComputed ? "computed" : ""}`}><span>Line {line}</span><div className="money-input"><span aria-hidden="true">$</span><input aria-label={`Form line ${line}`} type="number" step="0.01" inputMode="decimal" value={value === null || value === undefined ? "" : String(value)} readOnly={isComputed} onChange={(event) => update(`line${line}`, event.target.value === "" ? null : Number(event.target.value))} /></div><FieldErrors errors={errors} /></label>;
}

export function ComputedLines({ lines, form }: { lines: string[]; form: FormData }) {
  return <div className="computed-grid">{lines.map(line => <div className="computed-value" key={line}><span>Calculated line {line}</span><strong>{money(Number(form[`line${line}`] ?? 0))}</strong><small>Calculated automatically</small></div>)}</div>;
}