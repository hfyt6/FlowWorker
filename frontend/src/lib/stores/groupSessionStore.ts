import { writable } from 'svelte/store';
import { sessionApi } from '$lib/services/api';
import type { SessionListItemDto, GroupSessionDetailDto, CreateGroupSessionRequest } from '$lib/types';

// State
export const groupSessions = writable<SessionListItemDto[]>([]);
export const currentGroupSession = writable<GroupSessionDetailDto | null>(null);
export const loading = writable(false);
export const error = writable<string | null>(null);

// Fetch group sessions
export async function fetchGroupSessions() {
    loading.set(true);
    error.set(null);
    try {
        const sessions = await sessionApi.getGroupSessions();
        groupSessions.set(sessions);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to fetch group sessions');
        console.error('Failed to fetch group sessions:', err);
    } finally {
        loading.set(false);
    }
}

// Fetch group session detail
export async function fetchGroupSessionDetail(id: string) {
    loading.set(true);
    error.set(null);
    try {
        const session = await sessionApi.getGroupSession(id);
        currentGroupSession.set(session);
        return session;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to fetch group session detail');
        console.error('Failed to fetch group session detail:', err);
        return null;
    } finally {
        loading.set(false);
    }
}

// Create group session
export async function createGroupSession(request: CreateGroupSessionRequest): Promise<string> {
    loading.set(true);
    error.set(null);
    try {
        const id = await sessionApi.createGroupSession(request);
        await fetchGroupSessions();
        return id;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to create group session');
        console.error('Failed to create group session:', err);
        throw err;
    } finally {
        loading.set(false);
    }
}

// Delete group session
export async function deleteGroupSession(id: string) {
    loading.set(true);
    error.set(null);
    try {
        await sessionApi.deleteSession(id);
        groupSessions.update(sessions => sessions.filter(s => s.id !== id));
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to delete group session');
        console.error('Failed to delete group session:', err);
        throw err;
    } finally {
        loading.set(false);
    }
}

// Add participant
export async function addParticipant(sessionId: string, memberId: string) {
    error.set(null);
    try {
        await sessionApi.addParticipant(sessionId, memberId);
        await fetchGroupSessionDetail(sessionId);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to add participant');
        console.error('Failed to add participant:', err);
        throw err;
    }
}

// Remove participant
export async function removeParticipant(sessionId: string, memberId: string) {
    error.set(null);
    try {
        await sessionApi.removeParticipant(sessionId, memberId);
        await fetchGroupSessionDetail(sessionId);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to remove participant');
        console.error('Failed to remove participant:', err);
        throw err;
    }
}
