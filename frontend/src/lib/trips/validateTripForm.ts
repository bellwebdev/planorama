export interface TripFormValues {
  name: string;
  description: string;
  locationName: string;
  stayAddress: string;
  startDate: string;
  endDate: string;
  timezone: string;
  defaultVotingWindowHours: number;
}

// Mirrors CreateTripRequestValidator/UpdateTripRequestValidator — server-side is the real
// gate, this is just fast feedback before a round trip.
export function validateTripForm(values: TripFormValues): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!values.name.trim()) errors.name = "Name is required.";
  else if (values.name.length > 200) errors.name = "Name must be 200 characters or fewer.";

  if (!values.locationName.trim()) errors.locationName = "Location is required.";
  else if (values.locationName.length > 200) errors.locationName = "Location must be 200 characters or fewer.";

  if (!values.stayAddress.trim()) errors.stayAddress = "Stay address is required.";
  else if (values.stayAddress.length > 300) errors.stayAddress = "Stay address must be 300 characters or fewer.";

  if (!values.timezone.trim()) errors.timezone = "Timezone is required.";

  if (!values.startDate) errors.startDate = "Start date is required.";
  if (!values.endDate) errors.endDate = "End date is required.";
  if (values.startDate && values.endDate && values.endDate < values.startDate) {
    errors.endDate = "End date must be on or after the start date.";
  }

  if (values.defaultVotingWindowHours <= 0) {
    errors.defaultVotingWindowHours = "Voting window must be greater than 0 hours.";
  }

  return errors;
}
