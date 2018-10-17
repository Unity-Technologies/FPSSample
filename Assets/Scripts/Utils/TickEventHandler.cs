public struct TickEventHandler   
{
	public TickEventHandler(float expirationDuration)
	{
		m_expirationDuration = expirationDuration;
		m_startTick = 0;
	}

	public bool Update(GameTime time, int startTick)
	{
		if (startTick > m_startTick)
		{
			m_startTick = startTick;
			return time.DurationSinceTick(m_startTick) < m_expirationDuration;
		}
		return false;
	}

	float m_expirationDuration;
	int m_startTick;
}
