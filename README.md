# StickItApp

StickItApp is a WPF desktop application for the HCI project topic: managing events, event types, tags, and a visual event map. The app is prepared for assignment **G 5 Y**.

## Implemented features

- CRUD for events, event types, and tags.
- CSV persistence for all editable data.
- Event type protection: a type cannot be deleted while an event uses it.
- Tag deletion removes only event-tag relations and leaves events intact.
- Event editor with code, name, location, description, cost, attendance, charitable flag, type, tags, image preview, current dates, and previous dates.
- Previous dates use date pickers, an Add button, a visible list, and Remove buttons.
- Image upload uses `OpenFileDialog` and accepts `.jpg`, `.jpeg`, and `.png`; uploaded images can be changed or removed.
- Event, type, and tag search/filter/reset/sort flows.
- Type and tag filters search code/id, name, and description.
- Advanced search by event name, description, tags, type, attendance, location, and charitable status.
- Map supports placing events, moving events, returning events to the unplaced list, removing selected events, clearing the map, and preventing overlap with a visible friendly error.
- Clicking an event on the map shows useful details and actions: Details, Edit, Delete, and Back to list.
- EN/SR language switch for the main visible labels.
- Light/dark theme switch with persisted settings.
- Confirmation dialogs before deleting events, types, tags, and before clearing the map.

## CSV persistence

The app uses `CsvDataService` and stores CSV files in the runtime `Data` directory under the build output, for example:

`StickItApp/bin/Debug/net8.0-windows/Data`

On startup, the service creates missing CSV files with headers. If the original generic sample data is detected, it is replaced with the StickItApp wireframe sample data. User-created data is loaded from CSV and saved back after create, update, delete, map, and personalization changes.

CSV files:

- `events.csv`
- `event_types.csv`
- `tags.csv`
- `event_tags.csv`
- `previous_dates.csv`
- `settings.csv`

## Sample data

Events:

- EXIT 2026, Novi Sad, Serbia
- NBA Finals 2026, New York, USA
- Oscars 2026, Los Angeles, USA
- Oktoberfest 2026, Munich, Germany
- Cannes Film Festival 2026, Cannes, France

Types:

- Music Festival
- Sports Event
- Movie Awards
- Tech Conference
- Cultural Fair

Tags:

- Family
- Night
- Outdoor
- Student
- Urban

## Build and run

From `StickItApp`:

```powershell
dotnet build
dotnet run
```

The project targets `net8.0-windows` and uses WPF.

## Keyboard shortcuts

- `Ctrl+F`: focus the primary search/filter field on supported pages.
- `Esc`: close the selected map details panel or return/cancel on supported pages.
- Reset buttons are available on list, search, and map screens for filter cleanup.

## Personalization

- Language can be switched between EN and SR from the top bar.
- Theme can be switched between light and dark from the top bar.
- Language, theme, last sort mode, and last search text are persisted in `settings.csv`.


