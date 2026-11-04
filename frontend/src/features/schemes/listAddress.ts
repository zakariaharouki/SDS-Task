// What the list page passes to the form through the browser history, so returning
// to the list keeps the filter and sort. A direct link to the form has neither.
export interface ListState {
  listSearch?: string
  saved?: boolean
}

export const listAddress = (state: unknown) =>
  `/${(state as ListState | null)?.listSearch ?? ''}`

export const wasSaved = (state: unknown) =>
  (state as ListState | null)?.saved === true
