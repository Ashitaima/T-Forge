export type PagedResponse<T> = {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type UserDto = {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  /** Шлях відносно кореня API, або null. */
  avatarUrl: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string;
};

export type TournamentDto = {
  id: number;
  name: string;
  description: string;
  game: string;
  startDate: string;
  endDate: string;
  maxTeams: number;
  currentTeams: number;
  status: string;
  prizePool: number;
  isActive: boolean;
  /** Закритий турнір: склад учасників визначає організатор. */
  isInviteOnly: boolean;
  createdAt: string;
  organizer?: UserDto | null;
};

export type CreateTournamentDto = {
  name: string;
  description: string;
  game: string;
  startDate: string;
  endDate: string;
  maxTeams: number;
  prizePool: number;
  isInviteOnly: boolean;
  /** Заповнює сервер за токеном; вручну вказує лише адміністратор. */
  organizerId?: number | null;
  // Статус новоствореного турніру завжди «Реєстрація» — його проставляє сервер.
};

export type UpdateTournamentDto = {
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  maxTeams: number;
  prizePool: number;
  isInviteOnly: boolean;
  status: string;
};

export type TeamDto = {
  id: number;
  name: string;
  tag: string;
  description: string;
  region: string;
  isActive: boolean;
  createdAt: string;
  captain?: UserDto | null;
  players: PlayerSummaryDto[];
};

export type TeamSummaryDto = {
  id: number;
  name: string;
  tag: string;
  region: string;
  isActive: boolean;
  captain?: UserDto | null;
};

export type CreateTeamDto = {
  name: string;
  tag: string;
  description: string;
  region: string;
  /** Заповнює сервер за токеном; вручну вказує лише адміністратор. */
  captainId?: number | null;
};

export type UpdateTeamDto = {
  name: string;
  tag: string;
  description: string;
  region: string;
};

export type PlayerDto = {
  id: number;
  userId: number;
  nickname: string;
  position: string;
  country: string;
  age: number;
  totalMatches: number;
  wins: number;
  losses: number;
  winRate: number;
  ranking: number;
  isActive: boolean;
  joinedAt: string;
  user?: UserDto | null;
  team?: TeamSummaryDto | null;
};

export type PlayerSummaryDto = {
  id: number;
  /** Акаунт гравця — щоб зіставити склад із Team.captain.id. */
  userId: number;
  nickname: string;
  position: string;
  country: string;
  isActive: boolean;
};

export type PlayerRowDto = {
  id: number;
  userId: number;
  nickname: string;
  position: string;
  country: string;
  isActive: boolean;
  avatarUrl: string | null;
  teamId: number | null;
  teamName: string | null;
  matches: number;
  wins: number;
  losses: number;
  winRate: number;
  kills: number;
  deaths: number;
  assists: number;
  kda: number;
  /** Найкращий рейтинг серед дисциплін, або null — гравець без турнірних матчів. */
  rating: number | null;
  ratingGame: string | null;
  ratingTier: string | null;
};

export type TeamRowDto = {
  id: number;
  name: string;
  tag: string;
  region: string;
  isActive: boolean;
  captainId: number;
  captainUsername: string | null;
  playerCount: number;
  played: number;
  wins: number;
  losses: number;
  winRate: number;
  titles: number;
  /** Найкращий рейтинг серед дисциплін, або null — команда без турнірних матчів. */
  rating: number | null;
  ratingGame: string | null;
  ratingTier: string | null;
};

export type CreatePlayerDto = {
  nickname: string;
  position: string;
  country: string;
  age: number;
  /** Заповнює сервер за токеном; вручну вказує лише адміністратор. */
  userId?: number | null;
};

export type UpdatePlayerDto = {
  nickname: string;
  position: string;
  country: string;
  age: number;
};

export type UpdateProfileDto = {
  firstName: string;
  lastName: string;
  email: string;
};

export type CreateFullPlayerDto = {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  nickname: string;
  position: string;
  country: string;
  age: number;
};

export type MatchPlayerDto = {
  id: number;
  playerId: number;
  teamId?: number | null;
  kills: number;
  deaths: number;
  assists: number;
  champion: string;
  isStarter: boolean;
  player?: PlayerSummaryDto | null;
};

export type MatchDto = {
  id: number;
  /** Null — товариський матч із виклику капітана. */
  tournamentId: number | null;
  scheduledAt: string;
  startedAt?: string | null;
  endedAt?: string | null;
  status: string;
  homeTeamScore: number;
  awayTeamScore: number;
  matchType: string;
  /** Дисципліна, успадкована від турніру. Клієнт її не задає. */
  game: string;
  round: number;
  format: string;
  notes: string;
  streamUrl: string | null;
  /** Сторінка матчу в зовнішньому трекері статистики. Необовʼязкове. */
  trackerUrl: string | null;
  createdAt: string;
  homeTeam?: TeamSummaryDto | null;
  awayTeam?: TeamSummaryDto | null;
  /** Капітани команд — ними визначається, хто веде товариський матч. */
  homeTeamCaptainId: number;
  awayTeamCaptainId: number;
  winnerTeam?: TeamSummaryDto | null;
  tournament?: TournamentDto | null;
  matchPlayers: MatchPlayerDto[];
};

export type CreateMatchDto = {
  tournamentId: number;
  homeTeamId: number;
  awayTeamId: number;
  scheduledAt: string;
  matchType?: string;
  format?: string;
  notes?: string;
  streamUrl?: string | null;
  trackerUrl?: string | null;
};

export type UpdateMatchDto = {
  scheduledAt: string;
  status: string;
  homeTeamScore: number;
  awayTeamScore: number;
  winnerTeamId?: number | null;
  notes: string;
  streamUrl?: string | null;
  trackerUrl?: string | null;
  startedAt?: string | null;
  endedAt?: string | null;
};

export type CreateMatchPlayerDto = {
  playerId: number;
  kills: number;
  deaths: number;
  assists: number;
  champion: string;
  isStarter: boolean;
};

export type UpdateMatchPlayerDto = {
  kills: number;
  deaths: number;
  assists: number;
  champion: string;
  isStarter: boolean;
};

export type UpdateScoreDto = {
  homeTeamScore: number;
  awayTeamScore: number;
};

export type TournamentStandingDto = {
  place: number;
  team?: TeamSummaryDto | null;
  outcome: string;
  played: number;
  wins: number;
  losses: number;
  stillPlaying: boolean;
};

export type AuthResponseDto = {
  token: string;
  user: UserDto;
  expiresAt: string;
};

export type GameStatsDto = {
  game: string;
  count: number;
};

export type TournamentStatsDto = {
  totalTournaments: number;
  activeTournaments: number;
  completedTournaments: number;
  registrationOpen: number;
  totalPrizePool: number;
  popularGames: GameStatsDto[];
};

export type StreakDto = {
  type: string; // "Win" | "Loss"
  count: number;
};

export type TeamSummaryStatsDto = {
  played: number;
  wins: number;
  losses: number;
  winRate: number;
  streak?: StreakDto | null;
};

export type PlayerProfileDto = {
  player?: PlayerDto | null;
  matches: number;
  wins: number;
  losses: number;
  winRate: number;
  kills: number;
  deaths: number;
  assists: number;
  kda: number;
  /** Рейтинг за кожною дисципліною, у якій гравець грав турнірні матчі. */
  ratings: RatingDto[];
};

/** Рейтинг в одній дисципліні. Порожній список = гравець ще без рейтингу. */
export type RatingDto = {
  game: string;
  rating: number;
  peak: number;
  matchesRated: number;
  tier: string;
  /** Місце в таблиці цієї дисципліни, з одиниці. Однакові рейтинги ділять місце. */
  rank: number;
  /** Скільки всього суб'єктів мають рейтинг у цій дисципліні. */
  totalRanked: number;
  updatedAt: string;
};

/** Точка на графіку рейтингу — одна зміна за один матч. */
export type RatingChangeDto = {
  matchId: number;
  game: string;
  delta: number;
  ratingBefore: number;
  ratingAfter: number;
  createdAt: string;
  opponentName: string | null;
  tournamentName: string | null;
  matchType: string;
};

/** Зміна рейтингу обох команд у конкретному матчі. */
export type MatchRatingDeltaDto = {
  matchId: number;
  game: string;
  homeDelta: number | null;
  awayDelta: number | null;
};

export type TournamentInvitationDirection = "Invite" | "Application";

export type TournamentInvitationStatus = "Pending" | "Accepted" | "Declined" | "Cancelled";

export type TournamentInvitationDto = {
  id: number;
  tournamentId: number;
  tournamentName: string;
  tournamentGame: string;
  teamId: number;
  teamName: string;
  teamTag: string;
  /** Потрібні клієнту, щоб показати дії саме тій стороні, яка має відповідати. */
  teamCaptainId: number;
  organizerId: number;
  direction: TournamentInvitationDirection;
  status: TournamentInvitationStatus;
  message: string;
  createdAt: string;
  respondedAt: string | null;
};

export type PlayerMatchDto = {
  matchId: number;
  scheduledAt: string;
  status: string;
  playedFor?: TeamSummaryDto | null;
  opponent?: TeamSummaryDto | null;
  teamScore: number;
  opponentScore: number;
  result: string; // "Win" | "Loss" | "Pending"
  tournamentName?: string | null;
  matchType: string;
  kills: number;
  deaths: number;
  assists: number;
  champion: string;
};

export type MatchChallengeStatus = "Pending" | "Accepted" | "Declined" | "Cancelled";

export type MatchChallengeDto = {
  id: number;
  challengerTeamId: number;
  challengerTeamName: string;
  challengerTeamTag: string;
  opponentTeamId: number;
  opponentTeamName: string;
  opponentTeamTag: string;
  game: string;
  proposedAt: string;
  format: string;
  message: string;
  status: MatchChallengeStatus;
  createdAt: string;
  respondedAt: string | null;
  matchId: number | null;
};

export type CreateMatchChallengeDto = {
  challengerTeamId: number;
  opponentTeamId: number;
  game: string;
  proposedAt: string;
  format: string;
  message: string;
};

export type MembershipRequestDirection = "Invite" | "Application";

export type MembershipRequestStatus = "Pending" | "Accepted" | "Declined" | "Cancelled";

export type MembershipRequestDto = {
  id: number;
  teamId: number;
  teamName: string;
  teamTag: string;
  playerId: number;
  playerNickname: string;
  playerPosition: string;
  playerUserId: number;
  direction: MembershipRequestDirection;
  status: MembershipRequestStatus;
  createdAt: string;
  respondedAt: string | null;
};
