import { writable } from 'svelte/store';
import { memberApi } from '$lib/services/memberApi';
import type { MemberListItemDto, MemberDetailDto } from '$lib/types/member';

// State
export const members = writable<MemberListItemDto[]>([]);
export const currentMember = writable<MemberDetailDto | null>(null);
export const loading = writable(false);
export const error = writable<string | null>(null);

// Fetch all members
export async function fetchMembers() {
    loading.set(true);
    error.set(null);
    try {
        const data = await memberApi.getMembers();
        members.set(data);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to fetch members');
        console.error('Failed to fetch members:', err);
    } finally {
        loading.set(false);
    }
}

// Fetch member detail
export async function fetchMemberDetail(id: string) {
    loading.set(true);
    error.set(null);
    try {
        const data = await memberApi.getMember(id);
        currentMember.set(data);
        return data;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to fetch member detail');
        console.error('Failed to fetch member detail:', err);
        return null;
    } finally {
        loading.set(false);
    }
}

// Create member
export async function createMember(request: { name: string; type: string; roleId: string; systemPrompt?: string; description?: string }): Promise<string> {
    loading.set(true);
    error.set(null);
    try {
        const id = await memberApi.createMember(request);
        await fetchMembers();
        return id;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to create member');
        console.error('Failed to create member:', err);
        throw err;
    } finally {
        loading.set(false);
    }
}

// Update member
export async function updateMember(id: string, request: { name: string; type: string; roleId: string; systemPrompt?: string; description?: string; status: string }) {
    loading.set(true);
    error.set(null);
    try {
        await memberApi.updateMember(id, request);
        await fetchMembers();
        if (request.name) {
            const current = await memberApi.getMember(id);
            currentMember.set(current);
        }
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to update member');
        console.error('Failed to update member:', err);
        throw err;
    } finally {
        loading.set(false);
    }
}

// Delete member
export async function deleteMember(id: string) {
    loading.set(true);
    error.set(null);
    try {
        await memberApi.deleteMember(id);
        members.update(items => items.filter(m => m.id !== id));
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Failed to delete member');
        console.error('Failed to delete member:', err);
        throw err;
    } finally {
        loading.set(false);
    }
}
