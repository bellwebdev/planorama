import { useState } from "react";
import * as suggestionsApi from "../../lib/api/suggestions";
import { formatDistance, formatDuration } from "../../lib/places/formatDistance";
import type { SuggestionResponse, VoteValue } from "../../types/api";
import { Button } from "../Button/Button";
import { ErrorBanner } from "../ErrorBanner/ErrorBanner";
import styles from "./SuggestionsPanel.module.css";

const STATUS_LABELS: Record<SuggestionResponse["status"], string> = {
  Voting: "Voting",
  Approved: "Approved",
  Discarded: "Discarded",
  Expired: "Expired",
};

interface SuggestionCardProps {
  suggestion: SuggestionResponse;
  onChange: (updated: SuggestionResponse) => void;
  /** Route distance/duration from the stay, if it's been fetched for this card. */
  route?: { distanceMeters: number; durationSeconds: number };
}

export function SuggestionCard({ suggestion, onChange, route }: SuggestionCardProps) {
  const [voting, setVoting] = useState<VoteValue | null>(null);
  const [voteError, setVoteError] = useState<unknown>(null);

  const isVotingOpen = suggestion.status === "Voting" && new Date(suggestion.votingClosesAt) > new Date();

  async function vote(value: VoteValue) {
    setVoting(value);
    setVoteError(null);
    try {
      onChange(await suggestionsApi.castVote(suggestion.id, { value }));
    } catch (err) {
      setVoteError(err);
    } finally {
      setVoting(null);
    }
  }

  return (
    <li className={styles.card}>
      <div className={styles.cardHeader}>
        <div>
          <p className={styles.cardTitle}>{suggestion.title}</p>
          {suggestion.address && <p className={styles.cardAddress}>{suggestion.address}</p>}
        </div>
        <span className={`${styles.statusBadge} ${styles[`status${suggestion.status}`]}`}>
          {STATUS_LABELS[suggestion.status]}
        </span>
      </div>

      {suggestion.description && <p className={styles.cardDescription}>{suggestion.description}</p>}

      <div className={styles.cardMeta}>
        <span>Suggested by {suggestion.suggestedByName}</span>
        {suggestion.proposedDate && (
          <span>
            {suggestion.proposedDate}
            {suggestion.proposedStartTime && ` at ${suggestion.proposedStartTime}`}
          </span>
        )}
        {suggestion.durationMinutes && <span>{formatDuration(suggestion.durationMinutes * 60)}</span>}
        {route && (
          <span>
            {formatDistance(route.distanceMeters)} · {formatDuration(route.durationSeconds)} from stay
          </span>
        )}
      </div>

      {isVotingOpen ? (
        <>
          <div className={styles.voteRow}>
            <Button
              variant={suggestion.yourVote === "Yes" ? "primary" : "secondary"}
              onClick={() => void vote("Yes")}
              disabled={voting !== null}
            >
              {voting === "Yes" ? "Voting…" : "Yes"}
            </Button>
            <Button
              variant={suggestion.yourVote === "No" ? "primary" : "secondary"}
              onClick={() => void vote("No")}
              disabled={voting !== null}
            >
              {voting === "No" ? "Voting…" : "No"}
            </Button>
            <span className={styles.closesAt}>
              Voting closes {new Date(suggestion.votingClosesAt).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })}
            </span>
          </div>
          <ErrorBanner error={voteError} />
        </>
      ) : (
        <p className={styles.closedHint}>Voting closed.</p>
      )}

      {suggestion.hasVoted ? (
        <div className={styles.tally}>
          <span className={styles.tallyCount}>
            {suggestion.yesCount} yes · {suggestion.noCount} no
          </span>
          <ul className={styles.voteList}>
            {suggestion.votes!.map((v) => (
              <li key={v.userId}>
                {v.displayName}: {v.value}
              </li>
            ))}
          </ul>
        </div>
      ) : (
        isVotingOpen && <p className={styles.hiddenHint}>Cast your vote to see how everyone else voted.</p>
      )}
    </li>
  );
}
