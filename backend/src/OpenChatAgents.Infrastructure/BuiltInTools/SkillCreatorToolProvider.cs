using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenChatAgents.Domain.Abstractions;
using OpenChatAgents.Domain.Models;
using OpenChatAgents.Domain.Repositories;

namespace OpenChatAgents.Infrastructure.BuiltInTools;

/// <summary>Built-in "criador de skills" tool — lets the LLM create a new Skill when the user asks for one in chat.</summary>
public class SkillCreatorToolProvider(ISkillRepository skillRepo) : IBuiltInToolProvider
{
    public string Key => "skill-creator";

    public IReadOnlyList<AITool> GetTools(BuiltInToolContext context) =>
    [
        AIFunctionFactory.Create(
            (string name, string description, string content) => CreateSkillAsync(name, description, content, context.UserId),
            new AIFunctionFactoryOptions
            {
                Name = "create_skill",
                Description = "Cria uma nova skill (instrução reutilizável, em markdown) na plataforma. Use quando o usuário pedir para criar/salvar uma skill.",
            }),
    ];

    private async Task<string> CreateSkillAsync(
        [Description("Nome curto e único da skill")] string name,
        [Description("Descrição de uma linha do que a skill faz")] string description,
        [Description("Conteúdo completo da skill em markdown")] string content,
        Guid userId)
    {
        var existing = await skillRepo.GetByNameAsync(name.Trim());
        if (existing is not null)
            return $"Já existe uma skill chamada '{name}'. Escolha outro nome.";

        var skill = new Skill
        {
            Name = name.Trim(),
            Description = description,
            Content = content,
            CreatedByUserId = userId,
        };
        await skillRepo.CreateAsync(skill);
        return $"Skill '{name}' criada com sucesso.";
    }
}
