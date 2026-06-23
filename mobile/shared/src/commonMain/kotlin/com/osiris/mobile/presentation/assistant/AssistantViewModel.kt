package com.osiris.mobile.presentation.assistant

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.osiris.mobile.core.result.OsirisResult
import com.osiris.mobile.data.sync.DataChangeBus
import com.osiris.mobile.data.sync.DataScope
import com.osiris.mobile.data.sync.observeDataChanges
import com.osiris.mobile.domain.model.AiConversationSummary
import com.osiris.mobile.domain.model.AiMessage
import com.osiris.mobile.domain.model.AiProposal
import com.osiris.mobile.domain.repository.AssistantRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AssistantUiState(
    val conversations: List<AiConversationSummary> = emptyList(),
    val isLoadingConversations: Boolean = true,
    val selectedConversationId: String? = null,
    val messages: List<AiMessage> = emptyList(),
    val proposals: List<AiProposal> = emptyList(),
    val isSending: Boolean = false,
    val error: String? = null,
)

sealed interface AssistantEvent {
    data class ShowMessage(val message: String) : AssistantEvent
}

class AssistantViewModel(
    private val repository: AssistantRepository,
    private val dataChangeBus: DataChangeBus,
) : ViewModel() {

    private val _state = MutableStateFlow(AssistantUiState())
    val state: StateFlow<AssistantUiState> = _state.asStateFlow()

    private val _events = Channel<AssistantEvent>(Channel.BUFFERED)
    val events = _events.receiveAsFlow()

    init {
        loadConversations()
        observeDataChanges(dataChangeBus, DataScope.Assistant) { loadConversations() }
    }

    fun loadConversations() {
        _state.update { it.copy(isLoadingConversations = true, error = null) }
        viewModelScope.launch {
            when (val result = repository.listConversations()) {
                is OsirisResult.Success -> _state.update {
                    it.copy(conversations = result.value, isLoadingConversations = false)
                }

                is OsirisResult.Failure -> _state.update {
                    it.copy(isLoadingConversations = false, error = result.error.message)
                }
            }
        }
    }

    fun newConversation() {
        _state.update { it.copy(selectedConversationId = null, messages = emptyList(), proposals = emptyList()) }
    }

    fun openConversation(id: String) {
        _state.update { it.copy(selectedConversationId = id, proposals = emptyList()) }
        viewModelScope.launch {
            when (val result = repository.getConversation(id)) {
                is OsirisResult.Success -> _state.update { it.copy(messages = result.value.messages) }
                is OsirisResult.Failure -> _events.send(AssistantEvent.ShowMessage(result.error.message))
            }
        }
    }

    fun send(message: String) {
        val text = message.trim()
        if (text.isEmpty() || _state.value.isSending) {
            return
        }

        _state.update { it.copy(isSending = true) }
        viewModelScope.launch {
            val current = _state.value.selectedConversationId
            val result = if (current == null) {
                repository.startConversation(text)
            } else {
                repository.sendMessage(current, text)
            }

            when (result) {
                is OsirisResult.Success -> {
                    val turn = result.value
                    val messages = when (val detail = repository.getConversation(turn.conversationId)) {
                        is OsirisResult.Success -> detail.value.messages
                        is OsirisResult.Failure -> _state.value.messages
                    }
                    _state.update {
                        it.copy(
                            selectedConversationId = turn.conversationId,
                            messages = messages,
                            proposals = turn.proposals,
                            isSending = false,
                        )
                    }
                }

                is OsirisResult.Failure -> {
                    _state.update { it.copy(isSending = false) }
                    _events.send(AssistantEvent.ShowMessage(result.error.message))
                }
            }
        }
    }

    fun confirm(proposalId: String) {
        viewModelScope.launch {
            when (val result = repository.confirmAction(proposalId)) {
                is OsirisResult.Success -> {
                    removeProposal(proposalId)
                    _events.send(AssistantEvent.ShowMessage("Lançamento registrado."))
                }

                is OsirisResult.Failure -> _events.send(AssistantEvent.ShowMessage(result.error.message))
            }
        }
    }

    fun reject(proposalId: String) {
        viewModelScope.launch {
            when (val result = repository.rejectAction(proposalId)) {
                is OsirisResult.Success -> removeProposal(proposalId)
                is OsirisResult.Failure -> _events.send(AssistantEvent.ShowMessage(result.error.message))
            }
        }
    }

    fun archive(id: String) {
        viewModelScope.launch {
            when (val result = repository.archiveConversation(id)) {
                is OsirisResult.Success ->
                    if (_state.value.selectedConversationId == id) {
                        newConversation()
                    }

                is OsirisResult.Failure -> _events.send(AssistantEvent.ShowMessage(result.error.message))
            }
        }
    }

    private fun removeProposal(proposalId: String) {
        _state.update { it.copy(proposals = it.proposals.filterNot { proposal -> proposal.id == proposalId }) }
    }
}
