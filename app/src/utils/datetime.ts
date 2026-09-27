const pad = (n: number) => String(n).padStart(2, '0')

/**
 * Formats a Date as a local datetime-local input value (YYYY-MM-DDTHH:MM).
 *
 * `datetime-local` inputs expect the user's local wall-clock time. Feeding
 * them a UTC value (e.g. `toISOString().slice(0, 16)`) makes the picker show
 * UTC and, on submit, reinterprets that value as local — stamping events
 * several hours off.
 */
export function toLocalDateTimeInputValue(date: Date): string {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}