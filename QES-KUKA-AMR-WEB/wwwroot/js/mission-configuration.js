(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {
        const missionManager = createResourceManager({
            singular: 'Mission Type',
            plural: 'Mission Types',
            handlers: {
                list: 'MissionTypes',
                create: 'CreateMissionType',
                update: 'UpdateMissionType',
                delete: 'DeleteMissionType'
            },
            ids: {
                loading: 'missionTypesLoading',
                error: 'missionTypesError',
                errorMessage: 'missionTypesErrorMessage',
                empty: 'missionTypesEmpty',
                emptyCreate: 'missionTypesEmptyCreate',
                retry: 'missionTypesRetry',
                tableWrapper: 'missionTypesTableWrapper',
                tableBody: 'missionTypesTableBody',
                feedback: 'missionTypeFeedback',
                openCreate: 'btnOpenCreateMissionType',
                form: 'missionTypeForm',
                modalEl: 'missionTypeModal',
                modalTitle: 'missionTypeModalTitle',
                submitBtn: 'missionTypeSubmit',
                submitLabel: 'missionTypeSubmitLabel',
                submitSpinner: 'missionTypeSubmittingSpinner',
                fieldId: 'missionTypeId',
                fieldDisplayName: 'missionTypeDisplayName',
                fieldActualValue: 'missionTypeActualValue',
                fieldDescription: 'missionTypeDescription',
                fieldIsActive: 'missionTypeIsActive'
            }
        });

        const robotManager = createResourceManager({
            singular: 'Robot Type',
            plural: 'Robot Types',
            handlers: {
                list: 'RobotTypes',
                create: 'CreateRobotType',
                update: 'UpdateRobotType',
                delete: 'DeleteRobotType'
            },
            ids: {
                loading: 'robotTypesLoading',
                error: 'robotTypesError',
                errorMessage: 'robotTypesErrorMessage',
                empty: 'robotTypesEmpty',
                emptyCreate: 'robotTypesEmptyCreate',
                retry: 'robotTypesRetry',
                tableWrapper: 'robotTypesTableWrapper',
                tableBody: 'robotTypesTableBody',
                feedback: 'robotTypeFeedback',
                openCreate: 'btnOpenCreateRobotType',
                form: 'robotTypeForm',
                modalEl: 'robotTypeModal',
                modalTitle: 'robotTypeModalTitle',
                submitBtn: 'robotTypeSubmit',
                submitLabel: 'robotTypeSubmitLabel',
                submitSpinner: 'robotTypeSubmittingSpinner',
                fieldId: 'robotTypeId',
                fieldDisplayName: 'robotTypeDisplayName',
                fieldActualValue: 'robotTypeActualValue',
                fieldDescription: 'robotTypeDescription',
                fieldIsActive: 'robotTypeIsActive'
            }
        });

        const shelfDecisionManager = createResourceManager({
            singular: 'Shelf Decision Rule',
            plural: 'Shelf Decision Rules',
            handlers: {
                list: 'ShelfDecisionRules',
                create: 'CreateShelfDecisionRule',
                update: 'UpdateShelfDecisionRule',
                delete: 'DeleteShelfDecisionRule'
            },
            ids: {
                loading: 'shelfDecisionLoading',
                error: 'shelfDecisionError',
                errorMessage: 'shelfDecisionErrorMessage',
                empty: 'shelfDecisionEmpty',
                emptyCreate: 'shelfDecisionEmptyCreate',
                retry: 'shelfDecisionRetry',
                tableWrapper: 'shelfDecisionTableWrapper',
                tableBody: 'shelfDecisionTableBody',
                feedback: 'shelfDecisionFeedback',
                openCreate: 'btnOpenCreateShelfDecision',
                form: 'shelfDecisionForm',
                modalEl: 'shelfDecisionModal',
                modalTitle: 'shelfDecisionModalTitle',
                submitBtn: 'shelfDecisionSubmit',
                submitLabel: 'shelfDecisionSubmitLabel',
                submitSpinner: 'shelfDecisionSubmittingSpinner',
                fieldId: 'shelfDecisionId',
                fieldDisplayName: 'shelfDecisionDisplayName',
                fieldActualValue: 'shelfDecisionActualValue',
                fieldDescription: 'shelfDecisionDescription',
                fieldIsActive: 'shelfDecisionIsActive'
            }
        });

        const resumeStrategyManager = createResourceManager({
            singular: 'Resume Strategy',
            plural: 'Resume Strategies',
            handlers: {
                list: 'ResumeStrategies',
                create: 'CreateResumeStrategy',
                update: 'UpdateResumeStrategy',
                delete: 'DeleteResumeStrategy'
            },
            ids: {
                loading: 'resumeStrategyLoading',
                error: 'resumeStrategyError',
                errorMessage: 'resumeStrategyErrorMessage',
                empty: 'resumeStrategyEmpty',
                emptyCreate: 'resumeStrategyEmptyCreate',
                retry: 'resumeStrategyRetry',
                tableWrapper: 'resumeStrategyTableWrapper',
                tableBody: 'resumeStrategyTableBody',
                feedback: 'resumeStrategyFeedback',
                openCreate: 'btnOpenCreateResumeStrategy',
                form: 'resumeStrategyForm',
                modalEl: 'resumeStrategyModal',
                modalTitle: 'resumeStrategyModalTitle',
                submitBtn: 'resumeStrategySubmit',
                submitLabel: 'resumeStrategySubmitLabel',
                submitSpinner: 'resumeStrategySubmittingSpinner',
                fieldId: 'resumeStrategyId',
                fieldDisplayName: 'resumeStrategyDisplayName',
                fieldActualValue: 'resumeStrategyActualValue',
                fieldDescription: 'resumeStrategyDescription',
                fieldIsActive: 'resumeStrategyIsActive'
            }
        });

        const areaManager = createResourceManager({
            singular: 'Area',
            plural: 'Areas',
            handlers: {
                list: 'Areas',
                create: 'CreateArea',
                update: 'UpdateArea',
                delete: 'DeleteArea'
            },
            ids: {
                loading: 'areasLoading',
                error: 'areasError',
                errorMessage: 'areasErrorMessage',
                empty: 'areasEmpty',
                emptyCreate: 'areasEmptyCreate',
                retry: 'areasRetry',
                tableWrapper: 'areasTableWrapper',
                tableBody: 'areasTableBody',
                feedback: 'areaFeedback',
                openCreate: 'btnOpenCreateArea',
                form: 'areaForm',
                modalEl: 'areaModal',
                modalTitle: 'areaModalTitle',
                submitBtn: 'areaSubmit',
                submitLabel: 'areaSubmitLabel',
                submitSpinner: 'areaSubmittingSpinner',
                fieldId: 'areaId',
                fieldDisplayName: 'areaDisplayName',
                fieldActualValue: 'areaActualValue',
                fieldDescription: 'areaDescription',
                fieldIsActive: 'areaIsActive'
            }
        });

        missionManager.initialize();
        robotManager.initialize();
        shelfDecisionManager.initialize();
        resumeStrategyManager.initialize();
        areaManager.initialize();
        setupTabs();
    });

    function createResourceManager(config) {
        const state = {
            items: [],
            mode: 'create',
            isSubmitting: false,
            feedbackTimer: null
        };

        const elements = mapElements(config.ids);
        const modal = elements.modalEl ? new bootstrap.Modal(elements.modalEl) : null;

        function initialize() {
            if (!elements.form) {
                return;
            }

            elements.openCreate?.addEventListener('click', () => openModal('create'));
            elements.emptyCreate?.addEventListener('click', () => openModal('create'));
            elements.retry?.addEventListener('click', () => loadItems());
            elements.form.addEventListener('submit', handleSubmit);

            if (elements.tableBody) {
                elements.tableBody.addEventListener('click', handleTableClick);
            }

            loadItems();
        }

        async function loadItems() {
            setState('loading');

            try {
                const response = await fetch(`?handler=${config.handlers.list}`, {
                    headers: { 'Accept': 'application/json' }
                });

                const payload = await parseJson(response);
                if (!response.ok || payload?.success === false) {
                    const message = payload?.message || `Failed to load ${config.plural.toLowerCase()}.`;
                    throw new Error(message);
                }

                state.items = Array.isArray(payload?.data) ? payload.data : [];
                renderItems();
            } catch (error) {
                console.error(`${config.plural} load error`, error);
                state.items = [];
                setState('error', error.message);
            }
        }

        function renderItems() {
            if (!elements.tableBody) {
                return;
            }

            if (!Array.isArray(state.items) || state.items.length === 0) {
                elements.tableBody.innerHTML = '';
                setState('empty');
                return;
            }

            const rows = state.items.map(item => createRowHtml(item)).join('');
            elements.tableBody.innerHTML = rows;
            setState('content');
        }

        function createRowHtml(item) {
            const description = item.description
                ? escapeHtml(item.description)
                : '<span class="text-muted">—</span>';

            const status = item.isActive
                ? '<span class="status-pill status-active">Active</span>'
                : '<span class="status-pill status-inactive">Inactive</span>';

            const created = formatDate(item.createdUtc);
            const updated = formatDate(item.updatedUtc);

            return `
                <tr data-id="${item.id}">
                    <td>${escapeHtml(item.displayName ?? '')}</td>
                    <td><code>${escapeHtml(item.actualValue ?? '')}</code></td>
                    <td>${description}</td>
                    <td>${status}</td>
                    <td>
                        <div class="text-muted small">Created: ${created}</div>
                        <div class="text-muted small">Updated: ${updated}</div>
                    </td>
                    <td class="actions-cell">
                        <button type="button" class="btn btn-link btn-sm" data-action="edit" data-id="${item.id}">Edit</button>
                        <button type="button" class="btn btn-link btn-sm text-danger" data-action="delete" data-id="${item.id}">Delete</button>
                    </td>
                </tr>
            `;
        }

        function handleTableClick(event) {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            const action = target.dataset.action;
            if (!action) {
                return;
            }

            const id = Number.parseInt(target.dataset.id ?? '', 10);
            if (!Number.isInteger(id)) {
                showFeedback(`Invalid ${config.singular.toLowerCase()} identifier.`, 'danger');
                return;
            }

            const entity = state.items.find(item => item.id === id);
            if (!entity) {
                showFeedback(`${config.singular} not found.`, 'danger');
                return;
            }

            if (action === 'edit') {
                openModal('edit', entity);
            } else if (action === 'delete') {
                confirmDelete(entity);
            }
        }

        function openModal(mode, entity) {
            state.mode = mode;
            resetForm();

            if (mode === 'edit' && entity) {
            elements.modalTitle.textContent = `Edit ${config.singular}`;
            elements.fieldDisplayName.value = entity.displayName ?? '';
            elements.fieldActualValue.value = entity.actualValue ?? '';
            if (elements.fieldDescription) {
                elements.fieldDescription.value = entity.description ?? '';
            }
            if (elements.fieldIsActive) {
                elements.fieldIsActive.checked = Boolean(entity.isActive);
            }
            elements.fieldId.value = entity.id;
            elements.submitLabel.textContent = 'Save changes';
            } else {
            elements.modalTitle.textContent = `Create ${config.singular}`;
            if (elements.fieldIsActive) {
                elements.fieldIsActive.checked = true;
            }
            elements.submitLabel.textContent = 'Create';
        }

        modal?.show();
        setTimeout(() => elements.fieldDisplayName?.focus(), 200);
    }

    function resetForm() {
        elements.form?.reset();
        elements.fieldId.value = '';
        if (elements.fieldDescription) {
            elements.fieldDescription.value = '';
        }
        if (elements.fieldIsActive) {
            elements.fieldIsActive.checked = true;
        }
    }

        async function handleSubmit(event) {
            event.preventDefault();

            if (state.isSubmitting) {
                return;
            }

            const displayName = elements.fieldDisplayName.value.trim();
            const actualValue = elements.fieldActualValue.value.trim();

            if (!displayName) {
                showFeedback('Display value is required.', 'danger');
                elements.fieldDisplayName.focus();
                return;
            }

            if (!actualValue) {
                showFeedback('Actual value is required.', 'danger');
                elements.fieldActualValue.focus();
                return;
            }

            const payload = {
                displayName,
                actualValue,
                description: getDescriptionValue(),
                isActive: Boolean(elements.fieldIsActive?.checked)
            };

            let endpoint = `?handler=${config.handlers.create}`;
            if (state.mode === 'edit') {
                payload.id = Number.parseInt(elements.fieldId.value ?? '', 10);
                endpoint = `?handler=${config.handlers.update}`;
            }

            try {
                setSubmitting(true);
                const response = await fetch(endpoint, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(payload)
                });

                const result = await parseJson(response);
                if (!response.ok || result?.success === false) {
                    const message = result?.message || `Failed to save ${config.singular.toLowerCase()}.`;
                    throw new Error(message);
                }

                modal?.hide();
                showFeedback(
                    state.mode === 'edit'
                        ? `${config.singular} updated.`
                        : `${config.singular} created.`,
                    'success');
                await loadItems();
            } catch (error) {
                console.error(`${config.singular} submit error`, error);
                showFeedback(error.message ?? `Failed to save ${config.singular.toLowerCase()}.`, 'danger');
            } finally {
                setSubmitting(false);
            }
        }

        function setSubmitting(isSubmitting) {
            state.isSubmitting = isSubmitting;
            if (elements.submitBtn) {
                elements.submitBtn.disabled = isSubmitting;
            }

            elements.submitSpinner?.classList.toggle('d-none', !isSubmitting);
        }

        function confirmDelete(entity) {
            const confirmed = window.confirm(`Delete ${config.singular.toLowerCase()} "${entity.displayName}"? This action cannot be undone.`);
            if (!confirmed) {
                return;
            }

            deleteItem(entity.id);
        }

        async function deleteItem(id) {
            try {
                const response = await fetch(`?handler=${config.handlers.delete}`, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ id })
                });

                const result = await parseJson(response);
                if (!response.ok || result?.success === false) {
                    const message = result?.message || `Failed to delete ${config.singular.toLowerCase()}.`;
                    throw new Error(message);
                }

                showFeedback(`${config.singular} deleted.`, 'success');
                await loadItems();
            } catch (error) {
                console.error(`${config.singular} delete error`, error);
                showFeedback(error.message ?? `Failed to delete ${config.singular.toLowerCase()}.`, 'danger');
            }
        }

        function setState(stateName, message) {
            toggleElement(elements.loading, stateName !== 'loading');
            toggleElement(elements.error, stateName !== 'error');
            toggleElement(elements.empty, stateName !== 'empty');
            toggleElement(elements.tableWrapper, stateName !== 'content');

            if (stateName === 'error' && elements.errorMessage) {
                elements.errorMessage.textContent = message || `Unable to load ${config.plural.toLowerCase()}.`;
            }
        }

        function showFeedback(message, type) {
            if (!elements.feedback) {
                return;
            }

            const alertTypes = ['alert-success', 'alert-danger', 'alert-warning', 'alert-info'];
            elements.feedback.classList.remove(...alertTypes, 'd-none');
            elements.feedback.classList.add(`alert-${type}`);
            elements.feedback.textContent = message;

            clearTimeout(state.feedbackTimer);
            state.feedbackTimer = setTimeout(() => {
                elements.feedback.classList.add('d-none');
            }, 4000);
        }

        function getDescriptionValue() {
            if (!elements.fieldDescription) {
                return null;
            }

            const value = elements.fieldDescription.value.trim();
            return value ? value : null;
        }

        return {
            initialize
        };
    }

    function mapElements(ids) {
        const entries = {};
        Object.entries(ids).forEach(([key, id]) => {
            entries[key] = document.getElementById(id);
        });
        return entries;
    }

    function setupTabs() {
        const tabs = Array.from(document.querySelectorAll('.mission-configuration__tab[role="tab"]'));
        const panels = Array.from(document.querySelectorAll('.mission-configuration__panel'));
        if (tabs.length === 0) {
            return;
        }

        tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                if (tab.disabled) {
                    return;
                }

                activateTab(tab, tabs, panels);
            });
        });

        const currentActive = tabs.find(tab => tab.classList.contains('mission-configuration__tab--active')) ?? tabs[0];
        activateTab(currentActive, tabs, panels);
    }

    function activateTab(selected, tabs, panels) {
        tabs.forEach(tab => {
            const isActive = tab === selected;
            tab.classList.toggle('mission-configuration__tab--active', isActive);
            tab.setAttribute('aria-selected', String(isActive));
            tab.setAttribute('tabindex', isActive ? '0' : '-1');
        });

        const targetId = selected.getAttribute('aria-controls');
        panels.forEach(panel => {
            const isActive = panel.id === targetId;
            panel.classList.toggle('mission-configuration__panel--active', isActive);
            panel.hidden = !isActive;
        });
    }

    function toggleElement(element, shouldHide) {
        if (!element) return;
        element.classList.toggle('d-none', shouldHide);
    }

    async function parseJson(response) {
        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    function formatDate(value) {
        if (!value) {
            return '—';
        }

        try {
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) {
                return value;
            }

            return date.toLocaleString(undefined, {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        } catch {
            return value;
        }
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return '';
        }

        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }
})();
