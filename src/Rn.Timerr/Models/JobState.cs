using Rn.Timerr.Enums;
using Rn.Timerr.Models.Entities;

namespace Rn.Timerr.Models;

class RunningJobState
{
  private readonly Dictionary<string, StateEntity> _state = new(StringComparer.InvariantCultureIgnoreCase);
  private readonly string _category;
  private readonly string _host;

  public RunningJobState(string category, string host)
  {
    _category = category;
    _host = host;
  }

  public RunningJobState(string category, string host, List<StateEntity> state)
    : this(category, host)
  {
    foreach (var stateEntity in state)
      _state[stateEntity.Key] = stateEntity;
  }

  public bool ContainsKey(string key) => _state.ContainsKey(key);

  public DateTime GetDateTimeValue(string key)
  {
    if (!ContainsKey(key))
      throw new Exception($"Unable to find DateTime key: {key}");

    if (_state[key].Type.ToLower() != ConfigType.DateTime)
      throw new Exception($"Config key '{key}' is not of type DateTime");

    if (DateTime.TryParse(_state[key].Value, out var parsed))
      return parsed;

    throw new Exception($"Unable to parse '{_state[key].Value}' as DateTime");
  }

  public void SetValue(string key, DateTime value)
  {
    if (!_state.ContainsKey(key))
      _state[key] = new StateEntity
      {
        Category = _category,
        Key = key,
        Host = _host,
        Type = ConfigType.DateTime
      };

    _state[key].Value = value.ToString("u");
  }

  public List<StateEntity> GetStateEntities() => _state.Keys.Select(key => _state[key]).ToList();
}
